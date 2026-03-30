using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Sql.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetroMania.Infrastructure.Sql.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id) =>
        await db.Users.FindAsync(id);

    public async Task<User?> GetByNameAsync(string name) =>
        await db.Users.FirstOrDefaultAsync(u => u.Name == name);

    public async Task<List<User>> GetAllAsync() =>
        await db.Users.OrderBy(u => u.CreatedAt).ToListAsync();

    public async Task<int> CountAsync() =>
        await db.Users.CountAsync();

    public async Task AddAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is not null)
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
    }
}
