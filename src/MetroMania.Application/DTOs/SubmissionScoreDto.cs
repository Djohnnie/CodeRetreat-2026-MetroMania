using System.Globalization;

namespace MetroMania.Application.DTOs;

public record SubmissionScoreDto(
    Guid LevelId,
    string LevelTitle,
    int SortOrder,
    int Score,
    Dictionary<string, string> LocalizedTitles)
{
    /// <summary>Returns the title for the current UI culture, falling back to LevelTitle.</summary>
    public string GetLocalizedTitle()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return LocalizedTitles.TryGetValue(lang, out var t) && !string.IsNullOrEmpty(t)
            ? t
            : LevelTitle;
    }
}
