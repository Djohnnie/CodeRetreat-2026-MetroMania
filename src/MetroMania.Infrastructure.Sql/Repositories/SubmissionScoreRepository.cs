using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Sql.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetroMania.Infrastructure.Sql.Repositories;

public class SubmissionScoreRepository(AppDbContext db) : ISubmissionScoreRepository
{
    public async Task<List<SubmissionScore>> GetBySubmissionIdAsync(Guid submissionId) =>
        await db.SubmissionScores
            .Where(s => s.SubmissionId == submissionId)
            .ToListAsync();

    public async Task<List<SubmissionScore>> GetBySubmissionIdsAsync(IEnumerable<Guid> submissionIds)
    {
        var ids = submissionIds.ToList();
        return await db.SubmissionScores
            .Where(s => ids.Contains(s.SubmissionId))
            .ToListAsync();
    }

    public async Task AddManyAsync(IEnumerable<SubmissionScore> scores)
    {
        db.SubmissionScores.AddRange(scores);
        await db.SaveChangesAsync();
    }
}
