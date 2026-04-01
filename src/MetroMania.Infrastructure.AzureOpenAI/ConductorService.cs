using MetroMania.Application.Interfaces;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;

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

    private readonly ConcurrentDictionary<string, ConversationState> _conversations = new();

    public async Task<string> ChatAsync(string conversationId, string userMessage, CancellationToken cancellationToken = default)
    {
        var state = _conversations.GetOrAdd(conversationId, _ => new ConversationState(SystemPrompt));

        await state.Lock.WaitAsync(cancellationToken);
        try
        {
            state.History.Add(new ChatMessage(ChatRole.User, userMessage));

            var response = await chatClient.GetResponseAsync(state.History, cancellationToken: cancellationToken);

            state.History.AddRange(response.Messages);

            return response.Text ?? string.Empty;
        }
        finally
        {
            state.Lock.Release();
        }
    }

    public void ClearConversation(string conversationId) =>
        _conversations.TryRemove(conversationId, out _);

    private sealed class ConversationState(string systemPrompt)
    {
        public List<ChatMessage> History { get; } =
        [
            new ChatMessage(ChatRole.System, systemPrompt)
        ];

        public SemaphoreSlim Lock { get; } = new(1, 1);
    }
}
