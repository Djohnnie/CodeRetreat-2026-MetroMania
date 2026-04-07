using MetroMania.Domain.Entities;

namespace MetroMania.Domain.Interfaces;

public interface ISubmissionRenderRepository
{
    Task<SubmissionRender?> GetBySubmissionAndLevelAsync(Guid submissionId, Guid levelId);
    Task<List<SubmissionRender>> GetBySubmissionIdAsync(Guid submissionId);
    Task AddAsync(SubmissionRender render);
    Task UpdateAsync(SubmissionRender render);
}
