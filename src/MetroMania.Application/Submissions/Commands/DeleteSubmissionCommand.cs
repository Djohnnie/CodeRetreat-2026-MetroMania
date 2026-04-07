using MediatR;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record DeleteSubmissionCommand(Guid SubmissionId) : IRequest;

public class DeleteSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    ISubmissionRenderRepository renderRepository,
    ICleanupQueueService cleanupQueueService)
    : IRequestHandler<DeleteSubmissionCommand>
{
    public async Task Handle(DeleteSubmissionCommand request, CancellationToken cancellationToken)
    {
        // Fetch render info before deleting from SQL so we can derive blob names for async cleanup
        var renders = await renderRepository.GetBySubmissionIdAsync(request.SubmissionId);
        var renderInfos = renders
            .Select(r => (r.LevelId, r.TotalFrames))
            .ToList();

        // Delete the submission — SQL cascade removes SubmissionScores and SubmissionRenders
        await submissionRepository.DeleteAsync(request.SubmissionId);

        // Enqueue async blob cleanup (svgz, json, zip) — processed by CleanupProcessor
        if (renderInfos.Count > 0)
            await cleanupQueueService.EnqueueCleanupAsync(request.SubmissionId, renderInfos, cancellationToken);
    }
}
