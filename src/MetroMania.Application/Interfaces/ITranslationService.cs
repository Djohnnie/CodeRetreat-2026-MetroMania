namespace MetroMania.Application.Interfaces;

/// <summary>Translations of a level's title and description into all supported languages.</summary>
public sealed record LevelTranslationResult(
    string TitleNl,
    string DescriptionNl,
    string TitleAr,
    string DescriptionAr);

public interface ITranslationService
{
    /// <summary>
    /// Translates English level title and description into Dutch (nl) and Arabic (ar).
    /// </summary>
    Task<LevelTranslationResult> TranslateLevelAsync(
        string titleEn,
        string descriptionEn,
        CancellationToken cancellationToken = default);
}
