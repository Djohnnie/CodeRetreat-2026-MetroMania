namespace MetroMania.Infrastructure.AzureOpenAI;

/// <summary>
/// Holds the system prompt template for the Conductor chatbot.
/// Loaded from <c>conductor-instructions.md</c> in the Web project's wwwroot.
/// Supports {botName}, {userName}, and {languageName} placeholders.
/// </summary>
public sealed record ConductorInstructions(string MarkdownTemplate);
