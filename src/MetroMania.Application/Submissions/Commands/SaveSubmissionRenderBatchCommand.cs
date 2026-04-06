using MediatR;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

/// <summary>
/// Saves a batch of rendered frames: uploads individual SVG/JSON blobs and persists render metadata.
/// Does NOT create ZIP archives — those are handled separately after all batches are saved.
/// </summary>
public record SaveSubmissionRenderBatchCommand(Guid SubmissionId, List<SaveSubmissionRenderBatchCommand.LevelRender> Renders) : IRequest
{
    public record LevelRender(Guid LevelId, int Hour, string SvgContent, string JsonContent);
}

public class SaveSubmissionRenderBatchCommandHandler(
    ISubmissionRenderRepository renderRepository,
    IRenderBlobStorage blobStorage)
    : IRequestHandler<SaveSubmissionRenderBatchCommand>
{
    public async Task Handle(SaveSubmissionRenderBatchCommand request, CancellationToken cancellationToken)
    {
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
    }
}
