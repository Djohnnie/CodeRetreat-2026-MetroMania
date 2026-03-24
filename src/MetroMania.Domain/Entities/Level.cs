namespace MetroMania.Domain.Entities;

public class Level
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int GridWidth { get; set; } = 10;
    public int GridHeight { get; set; } = 8;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public LevelData LevelData { get; set; } = new();
}
