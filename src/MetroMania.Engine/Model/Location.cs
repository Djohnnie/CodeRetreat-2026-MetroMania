using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetroMania.Engine.Model;

/// <summary>
/// Represents a position on the game grid as integer tile coordinates.
/// </summary>
/// <param name="X">The horizontal tile coordinate (column).</param>
/// <param name="Y">The vertical tile coordinate (row).</param>
public record struct Location(int X, int Y);

/// <summary>
/// Serializes <see cref="Location"/> as a JSON property key in the format "X,Y".
/// Required because System.Text.Json cannot use non-string types as dictionary keys without this converter.
/// </summary>
public class LocationJsonConverter : JsonConverter<Location>
{
    /// <inheritdoc />
    public override Location Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var parts = reader.GetString()!.Split(',');
        return new Location(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Location value, JsonSerializerOptions options)
        => writer.WriteStringValue($"{value.X},{value.Y}");

    /// <inheritdoc />
    public override Location ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var parts = reader.GetString()!.Split(',');
        return new Location(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, Location value, JsonSerializerOptions options)
        => writer.WritePropertyName($"{value.X},{value.Y}");
}
