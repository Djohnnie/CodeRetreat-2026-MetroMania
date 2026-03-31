namespace MetroMania.Domain.Entities;

public class SubmissionRender
{
    public Guid Id { get; set; }
    public int SysId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid LevelId { get; set; }
    public int Hour { get; set; }
    public string SvgContent { get; set; } = string.Empty;

    public Submission Submission { get; set; } = null!;
    public Level Level { get; set; } = null!;
}
