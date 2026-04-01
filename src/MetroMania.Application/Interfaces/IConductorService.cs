using MetroMania.Application.DTOs;

namespace MetroMania.Application.Interfaces;

public interface IConductorService
{
    /// <summary>Sends a user message to Conductor, providing prior conversation history, and returns the assistant reply.</summary>
    Task<string> ChatAsync(IReadOnlyList<ChatMessageDto> history, string userMessage, CancellationToken cancellationToken = default);
}
