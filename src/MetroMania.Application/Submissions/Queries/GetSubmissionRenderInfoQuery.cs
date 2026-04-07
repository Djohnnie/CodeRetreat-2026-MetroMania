using MediatR;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record SubmissionRenderInfoDto(Guid Id, Guid SubmissionId, Guid LevelId, int TotalFrames);

public record GetSubmissionRenderInfoQuery(Guid SubmissionId, Guid LevelId) : IRequest<SubmissionRenderInfoDto?>;

public class GetSubmissionRenderInfoQueryHandler(ISubmissionRenderRepository renderRepository)
    : IRequestHandler<GetSubmissionRenderInfoQuery, SubmissionRenderInfoDto?>
{
    public async Task<SubmissionRenderInfoDto?> Handle(GetSubmissionRenderInfoQuery request, CancellationToken cancellationToken)
    {
        var render = await renderRepository.GetBySubmissionAndLevelAsync(request.SubmissionId, request.LevelId);
        return render is null
            ? null
            : new SubmissionRenderInfoDto(render.Id, render.SubmissionId, render.LevelId, render.TotalFrames);
    }
}
