using MediatR;
using MetroMania.Application.Levels.Commands;
using MetroMania.Application.Levels.Queries;
using MetroMania.Domain.Entities;

namespace MetroMania.Api.Endpoints;

public static class LevelEndpoints
{
    public static IEndpointRouteBuilder MapLevelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/levels").WithTags("Levels");

        group.MapGet("/", async (IMediator mediator) =>
        {
            var levels = await mediator.Send(new GetAllLevelsQuery());
            return Results.Ok(levels);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var level = await mediator.Send(new GetLevelQuery(id));
            return level is not null ? Results.Ok(level) : Results.NotFound();
        });

        group.MapPost("/", async (CreateLevelRequest request, IMediator mediator) =>
        {
            var level = await mediator.Send(new CreateLevelCommand(
                request.Title, request.Description, request.GridWidth, request.GridHeight));
            return Results.Created($"/api/levels/{level.Id}", level);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateLevelRequest request, IMediator mediator) =>
        {
            var level = await mediator.Send(new UpdateLevelCommand(
                id, request.Title, request.Description, request.GridWidth, request.GridHeight));
            return level is not null ? Results.Ok(level) : Results.NotFound();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var success = await mediator.Send(new DeleteLevelCommand(id));
            return Results.Ok(success);
        });

        group.MapPost("/{id:guid}/reorder", async (Guid id, ReorderLevelRequest request, IMediator mediator) =>
        {
            var success = await mediator.Send(new ReorderLevelCommand(id, request.Direction));
            return Results.Ok(success);
        });

        group.MapPut("/{id:guid}/grid-data", async (Guid id, UpdateGridDataRequest request, IMediator mediator) =>
        {
            var level = await mediator.Send(new UpdateGridDataCommand(id, request.LevelData));
            return level is not null ? Results.Ok(level) : Results.NotFound();
        });

        return app;
    }
}

record CreateLevelRequest(string Title, string Description, int GridWidth, int GridHeight);
record UpdateLevelRequest(string Title, string Description, int GridWidth, int GridHeight);
record ReorderLevelRequest(int Direction);
record UpdateGridDataRequest(LevelData LevelData);
