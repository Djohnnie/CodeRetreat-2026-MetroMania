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
        IConfiguration configuration)
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
                .AsIChatClient());

        services.AddSingleton<IConductorService, ConductorService>();

        return services;
    }
}
