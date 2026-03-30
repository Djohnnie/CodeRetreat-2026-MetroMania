using MetroMania.Domain.Entities;

namespace MetroMania.Domain.Interfaces;

public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id);
    Task<List<Submission>> GetByUserIdAsync(Guid userId);
    Task<int> GetNextVersionAsync(Guid userId);
    Task AddAsync(Submission submission);
    Task UpdateAsync(Submission submission);
    Task<Dictionary<Guid, (int Count, DateTime? LastSubmittedAt)>> GetSubmissionStatsByUserAsync();
}
