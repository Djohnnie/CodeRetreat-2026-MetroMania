using MetroMania.Application.Interfaces;

namespace MetroMania.Api.Endpoints;

public static class TranslationEndpoints
{
    public static IEndpointRouteBuilder MapTranslationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/translate").WithTags("Translation")
            .RequireAuthorization("Admin");

        group.MapPost("/level", async (TranslateLevelRequest request, ITranslationService translationService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.TitleEn))
                return Results.BadRequest("English title is required.");

            var result = await translationService.TranslateLevelAsync(
                request.TitleEn,
                request.DescriptionEn ?? string.Empty,
                ct);

            return Results.Ok(new TranslateLevelResponse(
                result.TitleNl,
                result.DescriptionNl,
                result.TitleAr,
                result.DescriptionAr));
        });

        return app;
    }
}

record TranslateLevelRequest(string TitleEn, string? DescriptionEn);
record TranslateLevelResponse(string TitleNl, string DescriptionNl, string TitleAr, string DescriptionAr);
