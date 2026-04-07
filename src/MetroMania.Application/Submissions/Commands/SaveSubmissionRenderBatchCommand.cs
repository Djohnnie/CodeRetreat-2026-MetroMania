using MediatR;
using MetroMania.Application.Helpers;
using MetroMania.Application.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

/// <summary>
/// Uploads a batch of rendered frames (SVGZ + JSON) to blob storage.
/// Does NOT write to the database — the render summary is saved separately via <see cref="SaveSubmissionRenderInfoCommand"/>.
/// </summary>
public record SaveSubmissionRenderBatchCommand(Guid SubmissionId, List<SaveSubmissionRenderBatchCommand.LevelRender> Renders) : IRequest
{
    public record LevelRender(Guid LevelId, int Hour, string SvgContent, string JsonContent);
}

public class SaveSubmissionRenderBatchCommandHandler(IRenderBlobStorage blobStorage)
    : IRequestHandler<SaveSubmissionRenderBatchCommand>
{
    public async Task Handle(SaveSubmissionRenderBatchCommand request, CancellationToken cancellationToken)
    {
        var uploadTasks = request.Renders
            .SelectMany(r => new[]
            {
                blobStorage.UploadBytesAsync($"{request.SubmissionId}_{r.LevelId}_{r.Hour:D4}.svgz",
                    SvgCompression.Compress(r.SvgContent), "image/svg+xml", cancellationToken),
                blobStorage.UploadAsync($"{request.SubmissionId}_{r.LevelId}_{r.Hour:D4}.json", r.JsonContent, cancellationToken)
            });
        await Task.WhenAll(uploadTasks);
    }
}
