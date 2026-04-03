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

    /// <summary>Passengers currently riding this train.</summary>
    public IReadOnlyList<Passenger> Passengers { get; init; } = [];
}