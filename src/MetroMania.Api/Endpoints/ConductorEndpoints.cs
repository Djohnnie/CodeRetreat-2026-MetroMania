using MediatR;
using MetroMania.Application.Conductor.Commands;
using MetroMania.Application.Conductor.Queries;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Enums;

namespace MetroMania.Api.Endpoints;

public static class ConductorEndpoints
{
    public static IEndpointRouteBuilder MapConductorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/conductor").WithTags("Conductor").RequireAuthorization();

        group.MapGet("/history/{userId:guid}", async (Guid userId, IMediator mediator) =>
        {
            var history = await mediator.Send(new GetChatHistoryQuery(userId));

            if (history.Count == 0)
            {
                const string welcome =
                    "👋 Hi! I'm **Conductor**, your metro network assistant. " +
                    "I can help you with game strategy, explain the rules, or review your bot code. " +
                    "What would you like to know?";
                var seeded = await mediator.Send(new SaveChatMessageCommand(userId, welcome, ChatMessageAuthor.Bot));
                history = [seeded];
            }

            return Results.Ok(history);
        });

        group.MapPost("/chat", async (ConductorChatRequest request, IConductorService conductor, IMediator mediator, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return Results.BadRequest("Message cannot be empty.");

            // Load prior history, then get AI reply
            var history = await mediator.Send(new GetChatHistoryQuery(request.UserId), ct);
            var reply = await conductor.ChatAsync(history, request.Message, ct);

            // Persist both turns
            await mediator.Send(new SaveChatMessageCommand(request.UserId, request.Message, ChatMessageAuthor.User), ct);
            await mediator.Send(new SaveChatMessageCommand(request.UserId, reply, ChatMessageAuthor.Bot), ct);

            return Results.Ok(new ConductorChatResponse(reply));
        });

        return app;
    }
}

record ConductorChatRequest(Guid UserId, string Message);
record ConductorChatResponse(string Reply);
