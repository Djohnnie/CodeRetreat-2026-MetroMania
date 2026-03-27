namespace MetroMania.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Version { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public List<SubmissionScore> Scores { get; set; } = [];
}
