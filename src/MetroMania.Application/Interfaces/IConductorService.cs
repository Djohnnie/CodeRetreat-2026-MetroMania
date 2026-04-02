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
    /// The <paramref name="onGetLatestCode"/> callback is invoked if the model calls the get_latest_submission_code tool;
    /// it should return the decoded C# source of the player's most recent submission, or <c>null</c> if none exists.
    /// The <paramref name="onGetLevelData"/> callback is invoked if the model calls the get_level_data tool;
    /// it receives the level title and should return a JSON string describing the level, or <c>null</c> if not found.
    /// </summary>
    Task<ConductorChatResult> ChatAsync(
        IReadOnlyList<ChatMessageDto> history,
        string userName,
        string language,
        string userMessage,
        IReadOnlyList<string> levelTitles,
        Func<CancellationToken, Task> onClearHistory,
        Func<int?, CancellationToken, Task<string?>> onGetLatestCode,
        Func<string, CancellationToken, Task<string?>> onGetLevelData,
        CancellationToken cancellationToken = default);
}
