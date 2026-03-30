using System.Globalization;

namespace MetroMania.Application.DTOs;

public record SubmissionScoreDto(
    Guid LevelId,
    string LevelTitle,
    string LevelDescription,
    int SortOrder,
    int Score,
    Dictionary<string, string> LocalizedTitles,
    Dictionary<string, string> LocalizedDescriptions)
{
    /// <summary>Returns the title for the current UI culture, falling back to LevelTitle.</summary>
    public string GetLocalizedTitle()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return LocalizedTitles.TryGetValue(lang, out var t) && !string.IsNullOrEmpty(t)
            ? t
            : LevelTitle;
    }

    /// <summary>Returns the description for the current UI culture, falling back to LevelDescription.</summary>
    public string GetLocalizedDescription()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return LocalizedDescriptions.TryGetValue(lang, out var d) && !string.IsNullOrEmpty(d)
            ? d
            : LevelDescription;
    }
}
