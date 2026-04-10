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

        group.MapGet("/{submissionId:guid}/render-levels", async (Guid submissionId, IMediator mediator) =>
        {
            var levels = await mediator.Send(new GetSubmissionRenderLevelsQuery(submissionId));
            return Results.Ok(levels);
        });

        group.MapGet("/{submissionId:guid}/levels/{levelId:guid}/renders", async (Guid submissionId, Guid levelId, IMediator mediator) =>
        {
            var renders = await mediator.Send(new GetSubmissionRendersQuery(submissionId, levelId));
            return Results.Ok(renders);
        });

        group.MapGet("/{submissionId:guid}/levels/{levelId:guid}/render-info", async (Guid submissionId, Guid levelId, IMediator mediator) =>
        {
            var info = await mediator.Send(new GetSubmissionRenderInfoQuery(submissionId, levelId));
            return info is null ? Results.NotFound() : Results.Ok(info);
        });

        group.MapGet("/{submissionId:guid}/levels/{levelId:guid}/renders/{hour:int}", async (Guid submissionId, Guid levelId, int hour, IMediator mediator) =>
        {
            var svg = await mediator.Send(new GetSubmissionRenderFrameQuery(submissionId, levelId, hour));
            return svg is null
                ? Results.NotFound()
                : Results.Content(svg, "image/svg+xml");
        });

        group.MapGet("/{submissionId:guid}/levels/{levelId:guid}/zip", async (Guid submissionId, Guid levelId, IMediator mediator) =>
        {
            var zipBytes = await mediator.Send(new GetSubmissionLevelZipQuery(submissionId, levelId));
            if (zipBytes is null)
                return Results.NotFound();

            return Results.File(zipBytes, "application/zip", $"{submissionId}_{levelId}.zip");
        });

        group.MapPost("/", async (SubmitCodeRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new SubmitCodeCommand(request.UserId, request.Code));

            if (!result.Success)
                return Results.BadRequest(new { errors = result.ValidationErrors });

            return Results.Created($"/api/submissions/{result.Submission!.Id}", result.Submission);
        });

        group.MapPost("/{id:guid}/rerun", async (Guid id, IMediator mediator) =>
        {
            var found = await mediator.Send(new RerunSubmissionCommand(id));
            return found ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteSubmissionCommand(id));
            return Results.NoContent();
        }).RequireAuthorization("Admin");

        // Internal endpoint for workers to broadcast submission status changes via SignalR
        group.MapPost("/notify", async (NotifySubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
        {
            await hubContext.Clients.Group(request.UserId.ToString())
                .SendAsync("SubmissionStatusChanged", request.SubmissionId,
                    request.RunStatus?.ToString() ?? "", request.RenderStatus?.ToString() ?? "");
            return Results.Ok();
        }).AllowAnonymous();

        return app;
    }
}

record SubmitCodeRequest(Guid UserId, string Code);
record NotifySubmissionRequest(Guid SubmissionId, Guid UserId, RunStatus? RunStatus, RenderStatus? RenderStatus);
