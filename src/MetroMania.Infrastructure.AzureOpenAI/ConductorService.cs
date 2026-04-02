using MetroMania.Application.DTOs;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Enums;
using Microsoft.Extensions.AI;

namespace MetroMania.Infrastructure.AzureOpenAI;

public sealed class ConductorService(IChatClient chatClient, ConductorInstructions instructions) : IConductorService
{
    public async Task<string> ChatAsync(
        IReadOnlyList<ChatMessageDto> history,
        string userName,
        string language,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var languageName = language == "nl" ? "Dutch" : "English";
        var botName = language == "nl" ? "Conducteur" : "Conductor";

        var systemPrompt = instructions.MarkdownTemplate
            .Replace("{botName}", botName)
            .Replace("{userName}", userName)
            .Replace("{languageName}", languageName);

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

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        return response.Text ?? string.Empty;
    }
}

