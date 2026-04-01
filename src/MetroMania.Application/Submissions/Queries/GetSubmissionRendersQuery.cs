using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetSubmissionRendersQuery(Guid SubmissionId, Guid LevelId) : IRequest<List<SubmissionRenderDto>>;

public class GetSubmissionRendersQueryHandler(
    ISubmissionRenderRepository renderRepository,
    IRenderBlobStorage blobStorage)
    : IRequestHandler<GetSubmissionRendersQuery, List<SubmissionRenderDto>>
{
    public async Task<List<SubmissionRenderDto>> Handle(GetSubmissionRendersQuery request, CancellationToken cancellationToken)
    {
        var renders = await renderRepository.GetBySubmissionAndLevelAsync(request.SubmissionId, request.LevelId);

        var downloads = renders
            .Where(r => !string.IsNullOrEmpty(r.SvgLocation))
            .Select(r => (r, task: blobStorage.DownloadAsync(r.SvgLocation, cancellationToken)))
            .ToList();

        await Task.WhenAll(downloads.Select(x => x.task));

        return downloads
            .Select(x => new SubmissionRenderDto(x.r.Id, x.r.SubmissionId, x.r.LevelId, x.r.Hour, x.task.Result))
            .ToList();
    }
}
