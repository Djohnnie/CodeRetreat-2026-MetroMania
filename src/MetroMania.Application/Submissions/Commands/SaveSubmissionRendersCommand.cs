using MediatR;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record SaveSubmissionRendersCommand(Guid SubmissionId, List<SaveSubmissionRendersCommand.LevelRender> Renders) : IRequest
{
    public record LevelRender(Guid LevelId, int Day, string SvgContent);
}

public class SaveSubmissionRendersCommandHandler(ISubmissionRenderRepository renderRepository)
    : IRequestHandler<SaveSubmissionRendersCommand>
{
    public async Task Handle(SaveSubmissionRendersCommand request, CancellationToken cancellationToken)
    {
        var entities = request.Renders.Select(r => new SubmissionRender
        {
            Id = Guid.NewGuid(),
            SubmissionId = request.SubmissionId,
            LevelId = r.LevelId,
            Day = r.Day,
            SvgContent = r.SvgContent
        });

        await renderRepository.AddManyAsync(entities);
    }
}
