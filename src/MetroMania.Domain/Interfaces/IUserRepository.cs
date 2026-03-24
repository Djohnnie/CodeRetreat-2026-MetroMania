using MetroMania.Domain.Entities;

namespace MetroMania.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByNameAsync(string name);
    Task<List<User>> GetAllAsync();
    Task<int> CountAsync();
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
}
