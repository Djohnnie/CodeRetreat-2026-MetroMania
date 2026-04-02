using MetroMania.Application.DTOs;

namespace MetroMania.Application.Interfaces;

/// <summary>The result of a single Conductor chat turn.</summary>
/// <param name="Reply">The assistant's text reply.</param>
/// <param name="HistoryCleared">True when the clear_chat_history tool was invoked during this turn.</param>
public sealed record ConductorChatResult(string Reply, bool HistoryCleared);

public interface IConductorService
{
    /// <summary>
    /// Sends a user message to Conductor and returns the assistant reply.
    /// The <paramref name="onClearHistory"/> callback is invoked if the model calls the clear_chat_history tool.
    /// </summary>
    Task<ConductorChatResult> ChatAsync(
        IReadOnlyList<ChatMessageDto> history,
        string userName,
        string language,
        string userMessage,
        Func<CancellationToken, Task> onClearHistory,
        CancellationToken cancellationToken = default);
}
