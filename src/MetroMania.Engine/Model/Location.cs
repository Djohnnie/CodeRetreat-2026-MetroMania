using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetroMania.Engine.Model;

public record struct Location(int X, int Y);

/// <summary>
/// Serializes <see cref="Location"/> as a JSON property key in the format "X,Y".
/// Required because System.Text.Json cannot use non-string types as dictionary keys without this converter.
/// </summary>
public class LocationJsonConverter : JsonConverter<Location>
{
    public override Location Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var parts = reader.GetString()!.Split(',');
        return new Location(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public override void Write(Utf8JsonWriter writer, Location value, JsonSerializerOptions options)
        => writer.WriteStringValue($"{value.X},{value.Y}");

    public override Location ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var parts = reader.GetString()!.Split(',');
        return new Location(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, Location value, JsonSerializerOptions options)
        => writer.WritePropertyName($"{value.X},{value.Y}");
}
