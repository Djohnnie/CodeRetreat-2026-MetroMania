using MetroMania.Domain.Entities;

namespace MetroMania.Domain.Interfaces;

public interface IChatMessageRepository
{
    Task<List<ChatMessage>> GetByUserIdAsync(Guid userId);
    Task AddAsync(ChatMessage message);
    Task DeleteAsync(Guid id);
    Task ArchiveAllByUserIdAsync(Guid userId);
}
