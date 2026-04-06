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

        // Derive JSON, ZIP blob names from SVG locations
        var allBlobNames = new List<string>();
        var zipBlobNames = new HashSet<string>();

        foreach (var loc in blobLocations.Where(l => !string.IsNullOrEmpty(l)))
        {
            allBlobNames.Add(loc); // SVG
            allBlobNames.Add(Path.ChangeExtension(loc, ".json")); // JSON

            // Derive ZIP name: {submissionId}_{levelId}.zip from {submissionId}_{levelId}_{hour}.svg
            var lastUnderscore = loc.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                var zipName = loc[..lastUnderscore] + ".zip";
                zipBlobNames.Add(zipName);
            }
        }

        allBlobNames.AddRange(zipBlobNames);

        // Delete the submission — SQL cascade removes SubmissionScores and SubmissionRenders
        await submissionRepository.DeleteAsync(request.SubmissionId);

        // Clean up blobs (best-effort: orphaned blobs are preferable to dangling DB records)
        if (allBlobNames.Count > 0)
            await Task.WhenAll(allBlobNames.Select(name => blobStorage.DeleteAsync(name, cancellationToken)));
    }
}
