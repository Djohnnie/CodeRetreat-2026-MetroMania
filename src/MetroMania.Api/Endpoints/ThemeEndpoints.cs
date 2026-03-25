using MediatR;
using MetroMania.Application.Theme.Commands;

namespace MetroMania.Api.Endpoints;

public static class ThemeEndpoints
{
    public static IEndpointRouteBuilder MapThemeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/theme").WithTags("Theme");

        group.MapPost("/toggle", async (ToggleThemeRequest request, IMediator mediator) =>
        {
            var isDarkMode = await mediator.Send(new ToggleThemeCommand(request.UserId));
            return Results.Ok(isDarkMode);
        });

        return app;
    }
}

record ToggleThemeRequest(Guid UserId);
