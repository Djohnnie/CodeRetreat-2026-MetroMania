using MetroMania.Application.DTOs;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Enums;
using Microsoft.Extensions.AI;

namespace MetroMania.Infrastructure.AzureOpenAI;

public sealed class ConductorService(IChatClient chatClient) : IConductorService
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

        var systemPrompt =
            $"""
            You are {botName}, an AI assistant for the MetroMania coding challenge.
            You are talking to {userName}. Always address them by their name to keep the conversation personal.
            MetroMania is a game where players write C# bot code to build metro networks,
            inspired by the Mini Metro game. Players implement the IMetroManiaRunner interface
            and earn points by efficiently connecting stations and transporting passengers.

            Help players understand the game rules, write better C# bot code, and optimize
            their metro strategies. Be concise, friendly, and encouraging.

            IMPORTANT: Always respond in {languageName}. Never switch languages.
            """;

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

