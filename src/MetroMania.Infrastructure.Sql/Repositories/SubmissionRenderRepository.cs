using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Sql.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetroMania.Infrastructure.Sql.Repositories;

public class SubmissionRenderRepository(AppDbContext db) : ISubmissionRenderRepository
{
    public async Task<SubmissionRender?> GetBySubmissionAndLevelAsync(Guid submissionId, Guid levelId) =>
        await db.SubmissionRenders
            .FirstOrDefaultAsync(r => r.SubmissionId == submissionId && r.LevelId == levelId);

    public async Task<List<SubmissionRender>> GetBySubmissionIdAsync(Guid submissionId) =>
        await db.SubmissionRenders
            .Where(r => r.SubmissionId == submissionId)
            .ToListAsync();

    public async Task AddAsync(SubmissionRender render)
    {
        db.SubmissionRenders.Add(render);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SubmissionRender render) =>
        await db.SaveChangesAsync();
}
