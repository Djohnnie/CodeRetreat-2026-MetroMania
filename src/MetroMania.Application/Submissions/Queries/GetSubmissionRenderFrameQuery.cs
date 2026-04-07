using MediatR;
using MetroMania.Application.Helpers;
using MetroMania.Application.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetSubmissionRenderFrameQuery(Guid SubmissionId, Guid LevelId, int Hour) : IRequest<string?>;

public class GetSubmissionRenderFrameQueryHandler(IRenderBlobStorage blobStorage)
    : IRequestHandler<GetSubmissionRenderFrameQuery, string?>
{
    public async Task<string?> Handle(GetSubmissionRenderFrameQuery request, CancellationToken cancellationToken)
    {
        var blobName = $"{request.SubmissionId}_{request.LevelId}_{request.Hour:D4}.svgz";
        try
        {
            var bytes = await blobStorage.DownloadBytesAsync(blobName, cancellationToken);
            return SvgCompression.Decompress(bytes);
        }
        catch
        {
            return null;
        }
    }
}
