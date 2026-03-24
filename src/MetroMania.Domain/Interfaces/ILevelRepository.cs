using MetroMania.Domain.Entities;

namespace MetroMania.Domain.Interfaces;

public interface ILevelRepository
{
    Task<Level?> GetByIdAsync(Guid id);
    Task<List<Level>> GetAllAsync();
    Task<int> GetMaxSortOrderAsync();
    Task AddAsync(Level level);
    Task UpdateAsync(Level level);
    Task UpdateManyAsync(IEnumerable<Level> levels);
    Task DeleteAsync(Guid id);
}
