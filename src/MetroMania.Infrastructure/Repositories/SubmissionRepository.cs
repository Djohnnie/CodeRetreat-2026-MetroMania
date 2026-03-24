using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetroMania.Infrastructure.Repositories;

public class SubmissionRepository(AppDbContext db) : ISubmissionRepository
{
    public async Task<List<Submission>> GetByUserIdAsync(Guid userId) =>
        await db.Submissions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Version)
            .ToListAsync();

    public async Task<int> GetNextVersionAsync(Guid userId) =>
        await db.Submissions.Where(s => s.UserId == userId).AnyAsync()
            ? await db.Submissions.Where(s => s.UserId == userId).MaxAsync(s => s.Version) + 1
            : 1;

    public async Task AddAsync(Submission submission)
    {
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();
    }
}
