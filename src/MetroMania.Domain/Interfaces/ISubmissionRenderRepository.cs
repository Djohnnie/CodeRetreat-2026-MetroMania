using MetroMania.Domain.Entities;

namespace MetroMania.Domain.Interfaces;

public interface ISubmissionRenderRepository
{
    Task<List<SubmissionRender>> GetBySubmissionAndLevelAsync(Guid submissionId, Guid levelId);
    Task<List<string>> GetLocationsBySubmissionIdAsync(Guid submissionId);
    Task AddManyAsync(IEnumerable<SubmissionRender> renders);
}
