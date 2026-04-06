using System.IO.Compression;
using System.Text;
using MediatR;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;
using MetroMania.Engine;

namespace MetroMania.Application.Submissions.Commands;

public record SaveSubmissionRendersCommand(Guid SubmissionId, List<SaveSubmissionRendersCommand.LevelRender> Renders) : IRequest
{
    public record LevelRender(Guid LevelId, string LevelTitle, int Hour, string SvgContent, string JsonContent);
}

public class SaveSubmissionRendersCommandHandler(
    ISubmissionRenderRepository renderRepository,
    IRenderBlobStorage blobStorage)
    : IRequestHandler<SaveSubmissionRendersCommand>
{
    public async Task Handle(SaveSubmissionRendersCommand request, CancellationToken cancellationToken)
    {
        // Build upload list for SVGs and JSONs
        var uploads = request.Renders
            .Select(r => (r,
                svgBlobName: $"{request.SubmissionId}_{r.LevelId}_{r.Hour:D4}.svg",
                jsonBlobName: $"{request.SubmissionId}_{r.LevelId}_{r.Hour:D4}.json"))
            .ToList();

        // Upload SVGs and JSONs in parallel
        var uploadTasks = uploads
            .SelectMany(x => new[]
            {
                blobStorage.UploadAsync(x.svgBlobName, x.r.SvgContent, cancellationToken),
                blobStorage.UploadAsync(x.jsonBlobName, x.r.JsonContent, cancellationToken)
            });
        await Task.WhenAll(uploadTasks);

        // Save render metadata to DB
        var entities = uploads.Select(x => new SubmissionRender
        {
            Id = Guid.NewGuid(),
            SubmissionId = request.SubmissionId,
            LevelId = x.r.LevelId,
            Hour = x.r.Hour,
            SvgLocation = x.svgBlobName
        });

        await renderRepository.AddManyAsync(entities);

        // Create ZIP per level containing all SVGs, JSONs, and viewer HTML
        var byLevel = request.Renders.GroupBy(r => r.LevelId);
        var zipTasks = byLevel.Select(async group =>
        {
            var rendersInOrder = group.OrderBy(r => r.Hour).ToList();
            var levelTitle = rendersInOrder[0].LevelTitle;

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var render in rendersInOrder)
                {
                    var svgEntry = archive.CreateEntry($"{render.Hour:D4}.svg", CompressionLevel.Optimal);
                    await using (var writer = new StreamWriter(svgEntry.Open(), Encoding.UTF8))
                        await writer.WriteAsync(render.SvgContent);

                    var jsonEntry = archive.CreateEntry($"{render.Hour:D4}.json", CompressionLevel.Optimal);
                    await using (var writer = new StreamWriter(jsonEntry.Open(), Encoding.UTF8))
                        await writer.WriteAsync(render.JsonContent);
                }

                // Include the viewer HTML so the renders can be viewed after extraction
                var viewerHtml = ViewerTemplate.Generate(levelTitle, rendersInOrder.Count, padWidth: 4);
                var viewerEntry = archive.CreateEntry("_viewer.html", CompressionLevel.Optimal);
                await using (var writer = new StreamWriter(viewerEntry.Open(), Encoding.UTF8))
                    await writer.WriteAsync(viewerHtml);
            }

            var zipBlobName = $"{request.SubmissionId}_{group.Key}.zip";
            await blobStorage.UploadBytesAsync(zipBlobName, memoryStream.ToArray(), "application/zip", cancellationToken);
        });

        await Task.WhenAll(zipTasks);
    }
}