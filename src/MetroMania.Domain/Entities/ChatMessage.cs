using MetroMania.Domain.Enums;

namespace MetroMania.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public int SysId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ChatMessageAuthor Author { get; set; }
    public bool IsArchived { get; set; } = false;

    public User User { get; set; } = null!;
}
