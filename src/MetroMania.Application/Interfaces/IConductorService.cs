namespace MetroMania.Application.Interfaces;

public interface IConductorService
{
    /// <summary>Sends a user message to Conductor and returns the assistant reply.</summary>
    Task<string> ChatAsync(string conversationId, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>Clears the stored conversation history for the given conversation ID.</summary>
    void ClearConversation(string conversationId);
}
