using System.Globalization;

namespace MetroMania.Application.DTOs;

public record SubmissionRenderLevelDto(
    Guid LevelId,
    string LevelTitle,
    int SortOrder,
    Dictionary<string, string> LocalizedTitles)
{
    public string GetLocalizedTitle()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return LocalizedTitles.TryGetValue(lang, out var t) && !string.IsNullOrEmpty(t)
            ? t
            : LevelTitle;
    }
}
