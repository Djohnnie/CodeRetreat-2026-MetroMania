using MetroMania.Application.DTOs;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Enums;
using Microsoft.Extensions.AI;

namespace MetroMania.Infrastructure.AzureOpenAI;

public sealed class ConductorService(IChatClient chatClient) : IConductorService
{
    private const string SystemPrompt =
        """
        You are Conductor, an AI assistant for the MetroMania coding challenge.
        MetroMania is a game where players write C# bot code to build metro networks,
        inspired by the Mini Metro game. Players implement the IMetroManiaRunner interface
        and earn points by efficiently connecting stations and transporting passengers.
        
        Help players understand the game rules, write better C# bot code, and optimize
        their metro strategies. Be concise, friendly, and encouraging.
        """;

    public async Task<string> ChatAsync(
        IReadOnlyList<ChatMessageDto> history,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt)
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

