using System.Globalization;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Application.DTOs;

public record LevelDto(
    Guid Id,
    string Title,
    string Description,
    int GridWidth,
    int GridHeight,
    int SortOrder,
    DateTime CreatedAt,
    string BackgroundColor,
    string WaterColor,
    int Seed,
    int VehicleCapacity,
    int MaxDays,
    Dictionary<string, LocalizedLevelText> LocalizedContent,
    List<MetroStation> Stations,
    List<Water> WaterTiles,
    List<WeeklyGiftOverride> WeeklyGiftOverrides,
    List<ResourceType> InitialResources)
{
    public static LevelDto FromEntity(Level level) =>
        new(level.Id, level.Title, level.Description, level.GridWidth, level.GridHeight,
            level.SortOrder, level.CreatedAt,
            level.LevelData.BackgroundColor, level.LevelData.WaterColor,
            level.LevelData.Seed, level.LevelData.VehicleCapacity, level.LevelData.MaxDays,
            level.LevelData.LocalizedContent,
            level.LevelData.Stations, level.LevelData.WaterTiles,
            level.LevelData.WeeklyGiftOverrides,
            level.LevelData.InitialResources);

    /// <summary>Returns the title for the current UI culture, falling back to the English Title.</summary>
    public string GetLocalizedTitle()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return LocalizedContent.TryGetValue(lang, out var t) && !string.IsNullOrEmpty(t.Title)
            ? t.Title
            : Title;
    }

    /// <summary>Returns the description for the current UI culture, falling back to the English Description.</summary>
    public string GetLocalizedDescription()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return LocalizedContent.TryGetValue(lang, out var t) && !string.IsNullOrEmpty(t.Description)
            ? t.Description
            : Description;
    }
}
