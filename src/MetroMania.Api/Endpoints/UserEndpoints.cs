using MediatR;
using MetroMania.Application.Users.Commands;
using MetroMania.Application.Users.Queries;
using MetroMania.Domain.Enums;

namespace MetroMania.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator) =>
        {
            var users = await mediator.Send(new GetAllUsersQuery());
            return Results.Ok(users);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var user = await mediator.Send(new GetUserByIdQuery(id));
            return user is not null ? Results.Ok(user) : Results.NotFound();
        });

        group.MapPost("/{id:guid}/approve", async (Guid id, ApproveUserRequest request, IMediator mediator) =>
        {
            var success = await mediator.Send(new ApproveUserCommand(id, request.NewStatus));
            return Results.Ok(success);
        }).RequireAuthorization("Admin");

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var success = await mediator.Send(new DeleteUserCommand(id));
            return Results.Ok(success);
        }).RequireAuthorization("Admin");

        return app;
    }
}

record ApproveUserRequest(ApprovalStatus NewStatus);
