using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Sql.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetroMania.Infrastructure.Sql.Repositories;

public class ChatMessageRepository(AppDbContext db) : IChatMessageRepository
{
    public async Task<List<ChatMessage>> GetByUserIdAsync(Guid userId) =>
        await db.ChatMessages
            .Where(m => m.UserId == userId && !m.IsArchived)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

    public async Task<List<ChatMessage>> GetAllByUserIdAsync(Guid userId) =>
        await db.ChatMessages
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

    public async Task AddAsync(ChatMessage message)
    {
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id) =>
        await db.ChatMessages.Where(m => m.Id == id).ExecuteDeleteAsync();

    public async Task ArchiveAllByUserIdAsync(Guid userId) =>
        await db.ChatMessages
            .Where(m => m.UserId == userId && !m.IsArchived)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsArchived, true));
}
