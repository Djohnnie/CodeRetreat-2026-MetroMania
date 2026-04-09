namespace MetroMania.Engine.Model;

/// <summary>
/// Represents a metro line connecting a sequence of stations on the map.
/// </summary>
public record Line
{
    /// <summary>Unique identifier for this line, matching the consumed line resource Id.</summary>
    public Guid LineId { get; init; }

    /// <summary>Display order index used to assign consistent colors and visual ordering to lines.</summary>
    public int OrderId { get; init; }

    /// <summary>Ordered list of station identifiers that this line connects, from first terminal to last.</summary>
    public IReadOnlyList<Guid> StationIds { get; init; } = [];

    /// <summary>
    /// When <c>true</c> the line is scheduled for removal. All trains on the line
    /// are also flagged for pending removal. Once every train has been removed
    /// (dropped all passengers), the line itself is removed and its resource released.
    /// </summary>
    public bool PendingRemoval { get; init; }
}