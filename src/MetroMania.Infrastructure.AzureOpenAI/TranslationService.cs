using MetroMania.Application.Interfaces;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetroMania.Infrastructure.AzureOpenAI;

public sealed class TranslationService(IChatClient chatClient) : ITranslationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<LevelTranslationResult> TranslateLevelAsync(
        string titleEn,
        string descriptionEn,
        CancellationToken cancellationToken = default)
    {
        const string systemPrompt =
            """
            You are a translation assistant. Translate the given level title and description into Dutch (nl) and Arabic (ar).
            Respond ONLY with a valid JSON object in this exact format, with no markdown, no code blocks, no explanation:
            {"titleNl":"...","descriptionNl":"...","titleAr":"...","descriptionAr":"..."}
            """;

        var userMessage = $"Title: {titleEn}\nDescription: {descriptionEn}";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userMessage)
        };

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        var raw = response.Text ?? string.Empty;

        // Strip any accidental markdown code fences
        var json = raw.Trim();
        if (json.StartsWith("```"))
        {
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
                json = json[start..(end + 1)];
        }

        var result = JsonSerializer.Deserialize<TranslationResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Translation service returned an invalid response.");

        return new LevelTranslationResult(
            result.TitleNl ?? string.Empty,
            result.DescriptionNl ?? string.Empty,
            result.TitleAr ?? string.Empty,
            result.DescriptionAr ?? string.Empty);
    }

    private sealed record TranslationResponse(
        string? TitleNl,
        string? DescriptionNl,
        string? TitleAr,
        string? DescriptionAr);
}
