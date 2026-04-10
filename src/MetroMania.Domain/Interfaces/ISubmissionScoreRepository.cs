using MetroMania.Domain.Entities;

namespace MetroMania.Domain.Interfaces;

public interface ISubmissionScoreRepository
{
    Task<List<SubmissionScore>> GetBySubmissionIdAsync(Guid submissionId);
    Task<List<SubmissionScore>> GetBySubmissionIdsAsync(IEnumerable<Guid> submissionIds);
    Task DeleteBySubmissionIdAsync(Guid submissionId);
    Task DeleteByLevelIdAsync(Guid levelId);
    Task AddManyAsync(IEnumerable<SubmissionScore> scores);
}
