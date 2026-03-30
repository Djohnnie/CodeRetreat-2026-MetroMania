using MediatR;
using MetroMania.Api.Hubs;
using MetroMania.Application.Submissions.Commands;
using MetroMania.Application.Submissions.Queries;
using MetroMania.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace MetroMania.Api.Endpoints;

public static class SubmissionEndpoints
{
    public static IEndpointRouteBuilder MapSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/submissions").WithTags("Submissions").RequireAuthorization();

        group.MapGet("/overviews", async (IMediator mediator) =>
        {
            var overviews = await mediator.Send(new GetAllSubmissionOverviewsQuery());
            return Results.Ok(overviews);
        }).RequireAuthorization("Admin");

        group.MapGet("/users/{userId:guid}", async (Guid userId, IMediator mediator) =>
        {
            var submissions = await mediator.Send(new GetUserSubmissionsQuery(userId));
            return Results.Ok(submissions);
        });

        group.MapGet("/starter-code", async (IMediator mediator) =>
        {
            var base64Code = await mediator.Send(new GetStarterCodeQuery());
            return Results.Ok(base64Code);
        });

        group.MapPost("/", async (SubmitCodeRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new SubmitCodeCommand(request.UserId, request.Code));

            if (!result.Success)
                return Results.BadRequest(new { errors = result.ValidationErrors });

            return Results.Created($"/api/submissions/{result.Submission!.Id}", result.Submission);
        });

        // Internal endpoint for the Worker to broadcast submission status changes via SignalR
        group.MapPost("/notify", async (NotifySubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
        {
            await hubContext.Clients.Group(request.UserId.ToString())
                .SendAsync("SubmissionStatusChanged", request.SubmissionId, request.Status.ToString());
            return Results.Ok();
        }).AllowAnonymous();

        return app;
    }
}

record SubmitCodeRequest(Guid UserId, string Code);
record NotifySubmissionRequest(Guid SubmissionId, Guid UserId, SubmissionStatus Status);
