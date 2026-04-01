using MediatR;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record SaveSubmissionRendersCommand(Guid SubmissionId, List<SaveSubmissionRendersCommand.LevelRender> Renders) : IRequest
{
    public record LevelRender(Guid LevelId, int Hour, string SvgContent);
}

public class SaveSubmissionRendersCommandHandler(
    ISubmissionRenderRepository renderRepository,
    IRenderBlobStorage blobStorage)
    : IRequestHandler<SaveSubmissionRendersCommand>
{
    public async Task Handle(SaveSubmissionRendersCommand request, CancellationToken cancellationToken)
    {
        var uploads = request.Renders
            .Select(r => (r, blobName: $"{request.SubmissionId}_{r.LevelId}_{r.Hour:D4}.svg"))
            .ToList();

        await Task.WhenAll(uploads.Select(x => blobStorage.UploadAsync(x.blobName, x.r.SvgContent, cancellationToken)));

        var entities = uploads.Select(x => new SubmissionRender
        {
            Id = Guid.NewGuid(),
            SubmissionId = request.SubmissionId,
            LevelId = x.r.LevelId,
            Hour = x.r.Hour,
            SvgLocation = x.blobName
        });

        await renderRepository.AddManyAsync(entities);
    }
}
