using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Domain.Interfaces;

public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id);
    Task<List<Submission>> GetByUserIdAsync(Guid userId);
    Task<int> GetNextVersionAsync(Guid userId);
    Task AddAsync(Submission submission);
    Task UpdateAsync(Submission submission);
    Task UpdateStatusFieldsAsync(Guid id, RunStatus? runStatus, RenderStatus? renderStatus, string? message);
    Task DeleteAsync(Guid id);
    Task<Dictionary<Guid, (int Count, DateTime? LastSubmittedAt)>> GetSubmissionStatsByUserAsync();
}
