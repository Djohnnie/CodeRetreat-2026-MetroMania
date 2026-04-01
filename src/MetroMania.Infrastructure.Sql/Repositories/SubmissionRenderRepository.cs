using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Sql.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetroMania.Infrastructure.Sql.Repositories;

public class SubmissionRenderRepository(AppDbContext db) : ISubmissionRenderRepository
{
    public async Task<List<SubmissionRender>> GetBySubmissionAndLevelAsync(Guid submissionId, Guid levelId) =>
        await db.SubmissionRenders
            .Where(r => r.SubmissionId == submissionId && r.LevelId == levelId)
            .OrderBy(r => r.Hour)
            .ToListAsync();

    public async Task<List<string>> GetLocationsBySubmissionIdAsync(Guid submissionId) =>
        await db.SubmissionRenders
            .Where(r => r.SubmissionId == submissionId)
            .Select(r => r.SvgLocation)
            .ToListAsync();

    public async Task AddManyAsync(IEnumerable<SubmissionRender> renders)
    {
        db.SubmissionRenders.AddRange(renders);
        await db.SaveChangesAsync();
    }
}
