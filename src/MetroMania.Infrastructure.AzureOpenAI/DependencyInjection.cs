using Azure;
using Azure.AI.OpenAI;
using MetroMania.Application.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ClientModel;

namespace MetroMania.Infrastructure.AzureOpenAI;

public static class DependencyInjection
{
    public static IServiceCollection AddAzureOpenAIConductor(
        this IServiceCollection services,
        IConfiguration configuration,
        string instructionsFilePath)
    {
        var endpoint = configuration["AZURE_OPENAI_ENDPOINT"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not configured.");
        var apiKey = configuration["AZURE_OPENAI_KEY"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_KEY is not configured.");
        var model = configuration["AZURE_OPENAI_MODEL"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_MODEL is not configured.");

        services.AddSingleton(_ =>
            new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
                .GetChatClient(model)
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build());

        var template = File.Exists(instructionsFilePath)
            ? File.ReadAllText(instructionsFilePath)
            : FallbackTemplate;
        services.AddSingleton(new ConductorInstructions(template));

        services.AddSingleton<IConductorService, ConductorService>();

        return services;
    }

    private const string FallbackTemplate =
        """
        You are {botName}, an AI assistant for the MetroMania coding challenge.
        You are talking to {userName}. Always address them by their name.
        Help players understand the game rules, write better C# bot code, and optimize
        their metro strategies. Be concise, friendly, and encouraging.
        IMPORTANT: Always respond in {languageName}. Never switch languages.
        """;
}
