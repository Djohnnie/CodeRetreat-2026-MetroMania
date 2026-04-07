using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Sql.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetroMania.Infrastructure.Sql.Repositories;

public class SubmissionRepository(AppDbContext db) : ISubmissionRepository
{
    public async Task<Submission?> GetByIdAsync(Guid id) =>
        await db.Submissions.FindAsync(id);

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

    public async Task UpdateAsync(Submission submission)
    {
        db.Submissions.Update(submission);
        await db.SaveChangesAsync();
    }

    public async Task UpdateStatusFieldsAsync(Guid id, RunStatus? runStatus, RenderStatus? renderStatus, string? message)
    {
        await db.Submissions
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(setters =>
            {
                if (runStatus.HasValue)
                    setters = setters.SetProperty(s => s.RunStatus, runStatus.Value);
                if (renderStatus.HasValue)
                    setters = setters.SetProperty(s => s.RenderStatus, renderStatus.Value);
                if (message is not null)
                    setters = setters.SetProperty(s => s.Message, message);
            });
    }

    public async Task DeleteAsync(Guid id) =>
        await db.Submissions.Where(s => s.Id == id).ExecuteDeleteAsync();

    public async Task<Dictionary<Guid, (int Count, DateTime? LastSubmittedAt)>>GetSubmissionStatsByUserAsync()
    {
        var stats = await db.Submissions
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastSubmittedAt = (DateTime?)g.Max(s => s.SubmittedAt) })
            .ToListAsync();

        return stats.ToDictionary(s => s.UserId, s => (s.Count, s.LastSubmittedAt));
    }
}
