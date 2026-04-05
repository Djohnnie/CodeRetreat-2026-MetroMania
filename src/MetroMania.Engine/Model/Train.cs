namespace MetroMania.Engine.Model;

public record Train
{
    /// <summary>Copied from the consumed Train resource Id.</summary>
    public Guid TrainId { get; init; }

    /// <summary>The line this train is assigned to.</summary>
    public Guid LineId { get; init; }

    /// <summary>Current tile the train occupies (grid coordinates).</summary>
    public Location TilePosition { get; init; }

    /// <summary>
    /// +1 = moving toward the end of the line path (higher indices);
    /// -1 = moving toward the start (lower indices). Reverses at terminals.
    /// </summary>
    public int Direction { get; init; } = 1;

    /// <summary>
    /// Index of the train's current position within the computed tile path of its line.
    /// Tracked explicitly to avoid ambiguity when duplicate tiles appear in the path
    /// (which can occur when the inbound and outbound paths to a turning-point station share tiles).
    /// -1 indicates the index has not been initialized; the engine will fall back to
    /// <see cref="List{T}.IndexOf"/> on that tick and then set the correct index going forward.
    /// </summary>
    public int PathIndex { get; init; } = -1;

    /// <summary>Passengers currently riding this train.</summary>
    public IReadOnlyList<Passenger> Passengers { get; init; } = [];
}