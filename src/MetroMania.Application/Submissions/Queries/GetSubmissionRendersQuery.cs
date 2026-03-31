using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetSubmissionRendersQuery(Guid SubmissionId, Guid LevelId) : IRequest<List<SubmissionRenderDto>>;

public class GetSubmissionRendersQueryHandler(ISubmissionRenderRepository renderRepository)
    : IRequestHandler<GetSubmissionRendersQuery, List<SubmissionRenderDto>>
{
    public async Task<List<SubmissionRenderDto>> Handle(GetSubmissionRendersQuery request, CancellationToken cancellationToken)
    {
        var renders = await renderRepository.GetBySubmissionAndLevelAsync(request.SubmissionId, request.LevelId);
        return renders
            .Select(r => new SubmissionRenderDto(r.Id, r.SubmissionId, r.LevelId, r.Hour, r.SvgContent))
            .ToList();
    }
}
