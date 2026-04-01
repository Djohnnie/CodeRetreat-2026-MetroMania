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
        // Fetch blob locations before deleting from SQL so we can clean them up afterwards
        var blobLocations = await renderRepository.GetLocationsBySubmissionIdAsync(request.SubmissionId);

        // Delete the submission — SQL cascade removes SubmissionScores and SubmissionRenders
        await submissionRepository.DeleteAsync(request.SubmissionId);

        // Clean up blobs (best-effort: orphaned blobs are preferable to dangling DB records)
        if (blobLocations.Count > 0)
            await Task.WhenAll(blobLocations
                .Where(loc => !string.IsNullOrEmpty(loc))
                .Select(loc => blobStorage.DeleteAsync(loc, cancellationToken)));
    }
}
