using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Application.DTOs;

public record ChatMessageDto(
    Guid Id,
    Guid UserId,
    string Content,
    DateTime Timestamp,
    ChatMessageAuthor Author,
    bool IsArchived)
{
    public static ChatMessageDto FromEntity(ChatMessage msg) =>
        new(msg.Id, msg.UserId, msg.Content, msg.Timestamp, msg.Author, msg.IsArchived);
}
