using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetSubmissionRenderLevelsQuery(Guid SubmissionId) : IRequest<List<SubmissionRenderLevelDto>>;

public class GetSubmissionRenderLevelsQueryHandler(
    ISubmissionRenderRepository renderRepository,
    ILevelRepository levelRepository)
    : IRequestHandler<GetSubmissionRenderLevelsQuery, List<SubmissionRenderLevelDto>>
{
    public async Task<List<SubmissionRenderLevelDto>> Handle(
        GetSubmissionRenderLevelsQuery request,
        CancellationToken cancellationToken)
    {
        var renders = await renderRepository.GetBySubmissionIdAsync(request.SubmissionId);
        var levels = await levelRepository.GetAllAsync();

        return renders
            .Select(r =>
            {
                var level = levels.FirstOrDefault(l => l.Id == r.LevelId);
                var localizedTitles = level?.LevelData.LocalizedContent
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Title)
                    ?? [];
                return new SubmissionRenderLevelDto(
                    r.LevelId,
                    level?.Title ?? "Unknown",
                    level?.SortOrder ?? 0,
                    localizedTitles);
            })
            .OrderBy(x => x.SortOrder)
            .ToList();
    }
}
