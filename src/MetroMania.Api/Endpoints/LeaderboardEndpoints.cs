using MediatR;
using MetroMania.Application.Submissions.Queries;

namespace MetroMania.Api.Endpoints;

public static class LeaderboardEndpoints
{
    public static IEndpointRouteBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/leaderboard", async (IMediator mediator) =>
        {
            var leaderboard = await mediator.Send(new GetLeaderboardQuery());
            return Results.Ok(leaderboard);
        }).WithTags("Leaderboard").RequireAuthorization();

        return app;
    }
}
