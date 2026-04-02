using MetroMania.Application.DTOs;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Enums;
using Microsoft.Extensions.AI;

namespace MetroMania.Infrastructure.AzureOpenAI;

public sealed class ConductorService(IChatClient chatClient, ConductorInstructions instructions) : IConductorService
{
    public async Task<ConductorChatResult> ChatAsync(
        IReadOnlyList<ChatMessageDto> history,
        string userName,
        string language,
        string userMessage,
        IReadOnlyList<string> levelTitles,
        Func<CancellationToken, Task> onClearHistory,
        Func<int?, CancellationToken, Task<string?>> onGetLatestCode,
        Func<string, CancellationToken, Task<string?>> onGetLevelData,
        CancellationToken cancellationToken = default)
    {
        var languageName = language switch { "nl" => "Dutch", "ar" => "Arabic", _ => "English" };
        var botName = language switch { "nl" => "Conducteur", "ar" => "القائد", _ => "Conductor" };

        var levelList = levelTitles.Count > 0
            ? string.Join(", ", levelTitles.Select(t => $"\"{t}\""))
            : "(no levels available)";

        var systemPrompt = instructions.MarkdownTemplate
            .Replace("{botName}", botName)
            .Replace("{userName}", userName)
            .Replace("{languageName}", languageName)
            .Replace("{levelList}", levelList);

        var historyCleared = false;

        // Local function used as the AI tool — captures closure vars so no parameters needed.
        async Task<string> DoClearHistory()
        {
            await onClearHistory(cancellationToken);
            historyCleared = true;
            return language switch
            {
                "nl" => "Gespreksgeschiedenis succesvol gearchiveerd.",
                "ar" => "تم أرشفة سجل المحادثة بنجاح.",
                _ => "Chat history successfully archived."
            };
        }

        async Task<string> DoGetLatestCode(int? version = null)
        {
            var code = await onGetLatestCode(version, cancellationToken);
            if (code is null)
                return language switch
                {
                    "nl" => "De speler heeft nog geen code ingediend.",
                    "ar" => "لم يقدم اللاعب أي كود بعد.",
                    _ => "The player has not submitted any code yet."
                };
            return code;
        }

        async Task<string> DoGetLevelData(string title)
        {
            var json = await onGetLevelData(title, cancellationToken);
            if (json is null)
                return language switch
                {
                    "nl" => $"Geen level gevonden met de titel '{title}'.",
                    "ar" => $"لم يُعثر على مستوى بعنوان '{title}'.",
                    _ => $"No level found with the title '{title}'."
                };
            return json;
        }

        var clearHistoryTool = AIFunctionFactory.Create(
            DoClearHistory,
            "clear_chat_history",
            "Archives all previous chat messages for the current user, giving them a fresh start. " +
            "Invoke this when the user explicitly asks to clear, wipe, reset, or start over their chat history.");

        var getLatestCodeTool = AIFunctionFactory.Create(
            DoGetLatestCode,
            "get_latest_submission_code",
            "Retrieves the player's submitted C# bot code. " +
            "Optionally pass a version number to fetch a specific submission; omit it (or pass null) to get the latest version. " +
            "Use this whenever the player refers to 'my code', asks for a review, " +
            "needs help debugging or improving it, or asks any question that requires seeing their actual code. " +
            "Never make assumptions about the code without fetching it first.");

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt)
        };

        foreach (var msg in history)
        {
            var role = msg.Author == ChatMessageAuthor.User ? ChatRole.User : ChatRole.Assistant;
            messages.Add(new ChatMessage(role, msg.Content));
        }

        messages.Add(new ChatMessage(ChatRole.User, userMessage));

        var getLevelDataTool = AIFunctionFactory.Create(
            DoGetLevelData,
            "get_level_data",
            "Retrieves the full JSON data for a specific level, identified by its exact title. " +
            "Use this when the player asks questions about a level's layout, stations, spawn rates, " +
            "weekly gifts, grid size, or any other level-specific detail.");

        // UseFunctionInvocation() middleware (registered in DI) handles the tool-call loop automatically.
        var options = new ChatOptions { Tools = [clearHistoryTool, getLatestCodeTool, getLevelDataTool] };
        var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);

        return new ConductorChatResult(response.Text ?? string.Empty, historyCleared);
    }
}

