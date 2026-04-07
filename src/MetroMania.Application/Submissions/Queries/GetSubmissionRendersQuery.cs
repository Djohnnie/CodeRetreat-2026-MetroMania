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
        var render = await renderRepository.GetBySubmissionAndLevelAsync(request.SubmissionId, request.LevelId);
        if (render is null || render.TotalFrames == 0)
            return [];

        var downloads = Enumerable.Range(1, render.TotalFrames)
            .Select(hour => (hour, task: blobStorage.DownloadAsync(
                $"{request.SubmissionId}_{request.LevelId}_{hour:D4}.svg", cancellationToken)))
            .ToList();

        await Task.WhenAll(downloads.Select(x => x.task));

        return downloads
            .Select(x => new SubmissionRenderDto(render.Id, request.SubmissionId, request.LevelId, x.hour, x.task.Result))
            .ToList();
    }
}
