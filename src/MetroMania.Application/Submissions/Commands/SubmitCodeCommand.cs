using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Extensions;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record SubmitCodeCommand(Guid UserId, string Code) : IRequest<SubmitCodeResult>;

public record SubmitCodeResult(bool Success, IReadOnlyList<string>? ValidationErrors, SubmissionDto? Submission);

public class SubmitCodeCommandHandler(
    ISubmissionRepository submissionRepository,
    IScriptValidationService scriptValidationService,
    ISubmissionQueueService submissionQueueService)
    : IRequestHandler<SubmitCodeCommand, SubmitCodeResult>
{
    public async Task<SubmitCodeResult> Handle(SubmitCodeCommand request, CancellationToken cancellationToken)
    {
        // Validate the script by compiling and running it before storing
        var base64Code = request.Code.Base64Encode();
        var validationResult = await scriptValidationService.ValidateAsync(base64Code);

        if (!validationResult.Success)
            return new SubmitCodeResult(false, validationResult.Errors, null);

        var nextVersion = await submissionRepository.GetNextVersionAsync(request.UserId);

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Version = nextVersion,
            Code = base64Code,
            SubmittedAt = DateTime.UtcNow
        };

        await submissionRepository.AddAsync(submission);

        // Enqueue submission for async processing on both queues
        await submissionQueueService.EnqueueRunAsync(submission.Id, cancellationToken);
        await submissionQueueService.EnqueueRenderAsync(submission.Id, cancellationToken);

        return new SubmitCodeResult(true, null, SubmissionDto.FromEntity(submission));
    }
}
