using System.Security.Claims;
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

        group.MapGet("/history/{userId:guid}", async (Guid userId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var history = await mediator.Send(new GetChatHistoryQuery(userId));

            if (history.Count == 0)
            {
                var language = user.FindFirst("Language")?.Value ?? "en";
                var welcome = language == "nl"
                    ? "👋 Hallo! Ik ben **Conducteur**, jouw metro netwerk assistent. " +
                      "Ik kan je helpen met spelstrategie, de regels uitleggen of je bot-code beoordelen. " +
                      "Wat wil je weten?"
                    : "👋 Hi! I'm **Conductor**, your metro network assistant. " +
                      "I can help you with game strategy, explain the rules, or review your bot code. " +
                      "What would you like to know?";
                var seeded = await mediator.Send(new SaveChatMessageCommand(userId, welcome, ChatMessageAuthor.Bot));
                history = [seeded];
            }

            return Results.Ok(history);
        });

        group.MapPost("/chat", async (ConductorChatRequest request, ClaimsPrincipal user, IConductorService conductor, IMediator mediator, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return Results.BadRequest("Message cannot be empty.");

            var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? "Player";
            var language = user.FindFirst("Language")?.Value ?? "en";

            // Load prior history, then get AI reply (tool may archive history during this call)
            var history = await mediator.Send(new GetChatHistoryQuery(request.UserId), ct);
            var result = await conductor.ChatAsync(
                history, userName, language, request.Message,
                onClearHistory: async (token) =>
                    await mediator.Send(new ArchiveChatHistoryCommand(request.UserId), token),
                ct);

            // Persist both turns (after any archiving has already happened)
            await mediator.Send(new SaveChatMessageCommand(request.UserId, request.Message, ChatMessageAuthor.User), ct);
            await mediator.Send(new SaveChatMessageCommand(request.UserId, result.Reply, ChatMessageAuthor.Bot), ct);

            return Results.Ok(new ConductorChatResponse(result.Reply, result.HistoryCleared));
        });

        return app;
    }
}

record ConductorChatRequest(Guid UserId, string Message);
record ConductorChatResponse(string Reply, bool HistoryCleared);
