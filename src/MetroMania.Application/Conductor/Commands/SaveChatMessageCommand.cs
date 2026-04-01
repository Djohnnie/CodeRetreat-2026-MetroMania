using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Conductor.Commands;

public record SaveChatMessageCommand(Guid UserId, string Content, ChatMessageAuthor Author) : IRequest<ChatMessageDto>;

public class SaveChatMessageCommandHandler(IChatMessageRepository repo)
    : IRequestHandler<SaveChatMessageCommand, ChatMessageDto>
{
    public async Task<ChatMessageDto> Handle(SaveChatMessageCommand request, CancellationToken cancellationToken)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Content = request.Content,
            Timestamp = DateTime.UtcNow,
            Author = request.Author,
            IsArchived = false
        };

        await repo.AddAsync(message);
        return ChatMessageDto.FromEntity(message);
    }
}
