using System.IO.Compression;
using System.Text;
using MediatR;
using MetroMania.Application.Interfaces;
using MetroMania.Engine;

namespace MetroMania.Application.Submissions.Commands;

/// <summary>
/// Creates ZIP archives per level by downloading the already-uploaded individual SVG/JSON blobs.
/// Called once after all render batches have been saved.
/// </summary>
public record CreateSubmissionRenderZipsCommand(
    Guid SubmissionId,
    List<CreateSubmissionRenderZipsCommand.LevelInfo> Levels) : IRequest
{
    public record LevelInfo(Guid LevelId, string LevelTitle, int TotalFrames);
}

public class CreateSubmissionRenderZipsCommandHandler(IRenderBlobStorage blobStorage)
    : IRequestHandler<CreateSubmissionRenderZipsCommand>
{
    public async Task Handle(CreateSubmissionRenderZipsCommand request, CancellationToken cancellationToken)
    {
        foreach (var level in request.Levels)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (var hour = 0; hour < level.TotalFrames; hour++)
                {
                    var svgBlobName = $"{request.SubmissionId}_{level.LevelId}_{hour:D4}.svg";
                    var svgContent = await blobStorage.DownloadAsync(svgBlobName, cancellationToken);
                    var svgEntry = archive.CreateEntry($"{hour:D4}.svg", CompressionLevel.Optimal);
                    await using (var writer = new StreamWriter(svgEntry.Open(), Encoding.UTF8))
                        await writer.WriteAsync(svgContent);

                    var jsonBlobName = $"{request.SubmissionId}_{level.LevelId}_{hour:D4}.json";
                    var jsonContent = await blobStorage.DownloadAsync(jsonBlobName, cancellationToken);
                    var jsonEntry = archive.CreateEntry($"{hour:D4}.json", CompressionLevel.Optimal);
                    await using (var writer = new StreamWriter(jsonEntry.Open(), Encoding.UTF8))
                        await writer.WriteAsync(jsonContent);
                }

                var viewerHtml = ViewerTemplate.Generate(level.LevelTitle, level.TotalFrames, padWidth: 4);
                var viewerEntry = archive.CreateEntry("_viewer.html", CompressionLevel.Optimal);
                await using (var writer = new StreamWriter(viewerEntry.Open(), Encoding.UTF8))
                    await writer.WriteAsync(viewerHtml);
            }

            var zipBlobName = $"{request.SubmissionId}_{level.LevelId}.zip";
            await blobStorage.UploadBytesAsync(zipBlobName, memoryStream.ToArray(), "application/zip", cancellationToken);
        }
    }
}
