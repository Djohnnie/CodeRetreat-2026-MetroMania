using MetroMania.Application.DTOs;

namespace MetroMania.Application.Interfaces;

/// <summary>The result of a single Conductor chat turn.</summary>
/// <param name="Reply">The assistant's text reply.</param>
/// <param name="HistoryCleared">True when the clear_chat_history tool was invoked during this turn.</param>
/// <param name="NavigateTo">Relative path to navigate to (e.g. "/play"), or null if no navigation was requested.</param>
/// <param name="ConductorClosed">True when the close_conductor tool was invoked during this turn.</param>
/// <param name="EditorCode">Updated C# code to inject into the Play page's Monaco editor, or null if no update was made.</param>
public sealed record ConductorChatResult(string Reply, bool HistoryCleared, string? NavigateTo = null, bool ConductorClosed = false, string? EditorCode = null);

public interface IConductorService
{
    /// <summary>
    /// Sends a user message to Conductor and returns the assistant reply.
    /// The <paramref name="onClearHistory"/> callback is invoked if the model calls the clear_chat_history tool.
    /// The <paramref name="onGetLatestCode"/> callback is invoked if the model calls the get_latest_submission_code tool;
    /// it should return the decoded C# source of the player's most recent submission, or <c>null</c> if none exists.
    /// The <paramref name="onGetLevelData"/> callback is invoked if the model calls the get_level_data tool;
    /// it receives the level title and should return a JSON string describing the level, or <c>null</c> if not found.
    /// The <paramref name="onGetLeaderboardPosition"/> callback is invoked if the model calls the get_leaderboard_position tool;
    /// it should return a summary of the player's score and rank, or <c>null</c> if no data is available.
    /// The <paramref name="editorCode"/> is the current content of the Play page's Monaco editor, or <c>null</c> if unavailable.
    /// </summary>
    Task<ConductorChatResult> ChatAsync(
        IReadOnlyList<ChatMessageDto> history,
        string userName,
        string language,
        string userMessage,
        IReadOnlyList<string> levelTitles,
        string? editorCode,
        Func<CancellationToken, Task> onClearHistory,
        Func<int?, CancellationToken, Task<string?>> onGetLatestCode,
        Func<string, CancellationToken, Task<string?>> onGetLevelData,
        Func<CancellationToken, Task<string?>> onGetLeaderboardPosition,
        CancellationToken cancellationToken = default);
}
