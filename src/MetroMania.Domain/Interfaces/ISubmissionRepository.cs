using MetroMania.Domain.Entities;

namespace MetroMania.Domain.Interfaces;

public interface ISubmissionRepository
{
    Task<List<Submission>> GetByUserIdAsync(Guid userId);
    Task<int> GetNextVersionAsync(Guid userId);
    Task AddAsync(Submission submission);
}
