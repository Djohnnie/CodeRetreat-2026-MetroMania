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
        Func<CancellationToken, Task> onClearHistory,
        CancellationToken cancellationToken = default)
    {
        var languageName = language == "nl" ? "Dutch" : "English";
        var botName = language == "nl" ? "Conducteur" : "Conductor";

        var systemPrompt = instructions.MarkdownTemplate
            .Replace("{botName}", botName)
            .Replace("{userName}", userName)
            .Replace("{languageName}", languageName);

        var historyCleared = false;

        // Local function used as the AI tool — captures closure vars so no parameters needed.
        async Task<string> DoClearHistory()
        {
            await onClearHistory(cancellationToken);
            historyCleared = true;
            return language == "nl"
                ? "Gespreksgeschiedenis succesvol gearchiveerd."
                : "Chat history successfully archived.";
        }

        var clearHistoryTool = AIFunctionFactory.Create(
            DoClearHistory,
            "clear_chat_history",
            "Archives all previous chat messages for the current user, giving them a fresh start. " +
            "Invoke this when the user explicitly asks to clear, wipe, reset, or start over their chat history.");

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

        // UseFunctionInvocation() middleware (registered in DI) handles the tool-call loop automatically.
        var options = new ChatOptions { Tools = [clearHistoryTool] };
        var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);

        return new ConductorChatResult(response.Text ?? string.Empty, historyCleared);
    }
}

