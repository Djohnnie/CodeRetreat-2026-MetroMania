using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Sql.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetroMania.Infrastructure.Sql.Repositories;

public class LevelRepository(AppDbContext db) : ILevelRepository
{
    public async Task<Level?> GetByIdAsync(Guid id) =>
        await db.Levels.FindAsync(id);

    public async Task<List<Level>> GetAllAsync() =>
        await db.Levels.OrderBy(l => l.SortOrder).ToListAsync();

    public async Task<int> GetMaxSortOrderAsync() =>
        await db.Levels.AnyAsync()
            ? await db.Levels.MaxAsync(l => l.SortOrder)
            : -1;

    public async Task AddAsync(Level level)
    {
        db.Levels.Add(level);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Level level)
    {
        db.Levels.Update(level);
        await db.SaveChangesAsync();
    }

    public async Task UpdateManyAsync(IEnumerable<Level> levels)
    {
        db.Levels.UpdateRange(levels);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var level = await db.Levels.FindAsync(id);
        if (level is not null)
        {
            db.Levels.Remove(level);
            await db.SaveChangesAsync();
        }
    }
}
