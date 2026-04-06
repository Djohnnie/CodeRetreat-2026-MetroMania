using MediatR;
using MetroMania.Application.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetSubmissionLevelZipQuery(Guid SubmissionId, Guid LevelId) : IRequest<byte[]?>;

public class GetSubmissionLevelZipQueryHandler(IRenderBlobStorage blobStorage)
    : IRequestHandler<GetSubmissionLevelZipQuery, byte[]?>
{
    public async Task<byte[]?> Handle(GetSubmissionLevelZipQuery request, CancellationToken cancellationToken)
    {
        var zipBlobName = $"{request.SubmissionId}_{request.LevelId}.zip";
        try
        {
            return await blobStorage.DownloadBytesAsync(zipBlobName, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
