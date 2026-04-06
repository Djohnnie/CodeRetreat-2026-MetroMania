namespace MetroMania.Engine.Model;

public record Line
{
    public Guid LineId { get; init; }
    public int OrderId { get; init; }
    public IReadOnlyList<Guid> StationIds { get; init; } = [];

    /// <summary>
    /// When <c>true</c> the line is scheduled for removal. All trains on the line
    /// are also flagged for pending removal. Once every train has been removed
    /// (dropped all passengers), the line itself is removed and its resource released.
    /// </summary>
    public bool PendingRemoval { get; init; }
}