using MediatR;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record DeleteSubmissionCommand(Guid SubmissionId) : IRequest;

public class DeleteSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    ISubmissionRenderRepository renderRepository,
    IRenderBlobStorage blobStorage)
    : IRequestHandler<DeleteSubmissionCommand>
{
    public async Task Handle(DeleteSubmissionCommand request, CancellationToken cancellationToken)
    {
        // Fetch render info before deleting from SQL so we can derive blob names for cleanup
        var renders = await renderRepository.GetBySubmissionIdAsync(request.SubmissionId);

        var allBlobNames = new List<string>();
        foreach (var render in renders)
        {
            for (var hour = 1; hour <= render.TotalFrames; hour++)
            {
                allBlobNames.Add($"{request.SubmissionId}_{render.LevelId}_{hour:D4}.svg");
                allBlobNames.Add($"{request.SubmissionId}_{render.LevelId}_{hour:D4}.json");
            }
            allBlobNames.Add($"{request.SubmissionId}_{render.LevelId}.zip");
        }

        // Delete the submission — SQL cascade removes SubmissionScores and SubmissionRenders
        await submissionRepository.DeleteAsync(request.SubmissionId);

        // Clean up blobs (best-effort: orphaned blobs are preferable to dangling DB records)
        if (allBlobNames.Count > 0)
            await Task.WhenAll(allBlobNames.Select(name => blobStorage.DeleteAsync(name, cancellationToken)));
    }
}
