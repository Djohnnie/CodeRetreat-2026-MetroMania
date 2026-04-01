using MetroMania.Application.Interfaces;

namespace MetroMania.Api.Endpoints;

public static class ConductorEndpoints
{
    public static IEndpointRouteBuilder MapConductorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/conductor").WithTags("Conductor").RequireAuthorization();

        group.MapPost("/chat", async (ConductorChatRequest request, IConductorService conductor, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return Results.BadRequest("Message cannot be empty.");

            var reply = await conductor.ChatAsync(request.ConversationId, request.Message, ct);
            return Results.Ok(new ConductorChatResponse(reply));
        });

        group.MapDelete("/chat/{conversationId}", (string conversationId, IConductorService conductor) =>
        {
            conductor.ClearConversation(conversationId);
            return Results.NoContent();
        });

        return app;
    }
}

record ConductorChatRequest(string ConversationId, string Message);
record ConductorChatResponse(string Reply);
