using MediatR;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MetroMania.Application.Submissions.Commands;

public record DeleteSubmissionCommand(Guid SubmissionId) : IRequest;

public class DeleteSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    ISubmissionRenderRepository renderRepository,
    ICleanupQueueService cleanupQueueService,
    ILogger<DeleteSubmissionCommandHandler> logger)
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

        // Enqueue async blob cleanup (svgz, json, zip) — processed by CleanupProcessor.
        // This is best-effort: a queue failure must not undo a successful DB delete.
        if (renderInfos.Count > 0)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                await cleanupQueueService.EnqueueCleanupAsync(request.SubmissionId, renderInfos, cts.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to enqueue blob cleanup for submission {SubmissionId}. Blobs may need manual cleanup.",
                    request.SubmissionId);
            }
        }
    }
}
