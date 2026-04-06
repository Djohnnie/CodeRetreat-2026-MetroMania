using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using MetroMania.Application.Conductor.Commands;
using MetroMania.Application.Conductor.Queries;
using MetroMania.Application.Interfaces;
using MetroMania.Application.Levels.Queries;
using MetroMania.Application.Submissions.Queries;
using MetroMania.Application.Users.Queries;
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
                var dbUser = await mediator.Send(new GetUserByIdQuery(userId));
                var language = dbUser?.Language ?? "en";
                var welcome = language switch
                {
                    "nl" => "👋 Hallo! Ik ben **Conducteur**, jouw metro netwerk assistent. " +
                            "Ik kan je helpen met spelstrategie, de regels uitleggen of je bot-code beoordelen. " +
                            "Wat wil je weten?",
                    "ar" => "👋 مرحباً! أنا **القائد**، مساعدك لشبكة المترو. " +
                            "يمكنني مساعدتك في استراتيجية اللعبة وشرح القواعد أو مراجعة كود البوت الخاص بك. " +
                            "ماذا تريد أن تعرف؟",
                    _ => "👋 Hi! I'm **Conductor**, your metro network assistant. " +
                         "I can help you with game strategy, explain the rules, or review your bot code. " +
                         "What would you like to know?"
                };
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

            var dbUser = await mediator.Send(new GetUserByIdQuery(request.UserId), ct);
            var language = dbUser?.Language ?? "en";

            // Load prior history, then get AI reply (tool may archive history during this call)
            var history = await mediator.Send(new GetChatHistoryQuery(request.UserId), ct);
            var allLevels = await mediator.Send(new GetAllLevelsQuery(), ct);
            var levelTitles = allLevels.Select(l => l.Title).ToList();
            var result = await conductor.ChatAsync(
                history, userName, language, request.Message, levelTitles,
                onClearHistory: async (token) =>
                    await mediator.Send(new ArchiveChatHistoryCommand(request.UserId), token),
                onGetLatestCode: async (version, token) =>
                {
                    var submissions = await mediator.Send(new GetUserSubmissionsQuery(request.UserId), token);
                    if (submissions.Count == 0) return null;
                    var match = version.HasValue
                        ? submissions.FirstOrDefault(s => s.Version == version.Value)
                        : submissions.MaxBy(s => s.Version);
                    return match?.Code;
                },
                onGetLevelData: async (title, token) =>
                {
                    var level = allLevels.FirstOrDefault(l =>
                        string.Equals(l.Title, title, StringComparison.OrdinalIgnoreCase));
                    if (level is null) return null;
                    return JsonSerializer.Serialize(level, ConductorSerializerOptions.LevelData);
                },
                onGetLeaderboardPosition: async (token) =>
                {
                    var leaderboard = await mediator.Send(new GetLeaderboardQuery(), token);
                    var entry = leaderboard.FirstOrDefault(e => e.UserId == request.UserId);
                    if (entry is null) return null;
                    var position = leaderboard.IndexOf(entry) + 1;
                    var totalPlayers = leaderboard.Count;
                    var levelBreakdown = string.Join(", ", entry.LevelScores
                        .OrderBy(s => s.SortOrder)
                        .Select(s => $"{s.LevelTitle}: {s.Score}"));
                    return $"Position: {position} of {totalPlayers} | Total score: {entry.TotalScore} | " +
                           $"Submissions: {entry.SubmissionCount} | Per-level scores: {levelBreakdown}";
                },
                ct);

            // Persist both turns (after any archiving has already happened)
            await mediator.Send(new SaveChatMessageCommand(request.UserId, request.Message, ChatMessageAuthor.User), ct);
            await mediator.Send(new SaveChatMessageCommand(request.UserId, result.Reply, ChatMessageAuthor.Bot), ct);

            return Results.Ok(new ConductorChatResponse(result.Reply, result.HistoryCleared, result.NavigateTo, result.ConductorClosed));
        });

        return app;
    }
}

record ConductorChatRequest(Guid UserId, string Message);
record ConductorChatResponse(string Reply, bool HistoryCleared, string? NavigateTo, bool ConductorClosed);

file static class ConductorSerializerOptions
{
    internal static readonly JsonSerializerOptions LevelData = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };
}
