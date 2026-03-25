using MediatR;
using MetroMania.Application.Submissions.Commands;
using MetroMania.Application.Submissions.Queries;

namespace MetroMania.Api.Endpoints;

public static class SubmissionEndpoints
{
    public static IEndpointRouteBuilder MapSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/submissions").WithTags("Submissions");

        group.MapGet("/overviews", async (IMediator mediator) =>
        {
            var overviews = await mediator.Send(new GetAllSubmissionOverviewsQuery());
            return Results.Ok(overviews);
        });

        group.MapGet("/users/{userId:guid}", async (Guid userId, IMediator mediator) =>
        {
            var submissions = await mediator.Send(new GetUserSubmissionsQuery(userId));
            return Results.Ok(submissions);
        });

        group.MapPost("/", async (SubmitCodeRequest request, IMediator mediator) =>
        {
            var submission = await mediator.Send(new SubmitCodeCommand(request.UserId, request.Code));
            return Results.Created($"/api/submissions/{submission.Id}", submission);
        });

        return app;
    }
}

record SubmitCodeRequest(Guid UserId, string Code);
