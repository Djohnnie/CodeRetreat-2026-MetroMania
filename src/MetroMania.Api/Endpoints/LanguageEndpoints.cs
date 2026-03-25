using MediatR;
using MetroMania.Application.Language.Commands;

namespace MetroMania.Api.Endpoints;

public static class LanguageEndpoints
{
    public static IEndpointRouteBuilder MapLanguageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/language").WithTags("Language").RequireAuthorization();

        group.MapPost("/change", async (ChangeLanguageRequest request, IMediator mediator) =>
        {
            var success = await mediator.Send(new ChangeLanguageCommand(request.UserId, request.Language));
            return Results.Ok(success);
        });

        return app;
    }
}

record ChangeLanguageRequest(Guid UserId, string Language);
