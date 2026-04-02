using MediatR;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Conductor.Commands;

public record ArchiveChatHistoryCommand(Guid UserId) : IRequest;

public class ArchiveChatHistoryCommandHandler(IChatMessageRepository repo)
    : IRequestHandler<ArchiveChatHistoryCommand>
{
    public Task Handle(ArchiveChatHistoryCommand request, CancellationToken cancellationToken) =>
        repo.ArchiveAllByUserIdAsync(request.UserId);
}
