namespace MetroMania.Domain.Entities;

public class SubmissionScore
{
    public Guid Id { get; set; }
    public int SysId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid LevelId { get; set; }
    public int Score { get; set; }

    public Submission Submission { get; set; } = null!;
    public Level Level { get; set; } = null!;
}
