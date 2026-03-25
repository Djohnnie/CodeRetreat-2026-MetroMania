using MediatR;
using MetroMania.Application.Auth.Commands;
using MetroMania.Application.Auth.Queries;

namespace MetroMania.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new LoginQuery(request.Name, request.Password));
            return Results.Ok(result);
        });

        group.MapPost("/register", async (RegisterRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new RegisterCommand(request.Name, request.Password));
            return Results.Ok(result);
        });

        return app;
    }
}

record LoginRequest(string Name, string Password);
record RegisterRequest(string Name, string Password);
