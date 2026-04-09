using Azure.Data.Tables;
using MetroMania.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MetroMania.Orleans.Client;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the Orleans client for script validation and registers the
    /// <see cref="IScriptValidationService"/> backed by the validation silo.
    /// </summary>
    public static IHostApplicationBuilder AddOrleansValidationClient(
        this IHostApplicationBuilder builder, bool useLocalhostClustering = false)
    {
        builder.UseOrleansClient(clientBuilder =>
        {
            if (useLocalhostClustering)
            {
                clientBuilder.UseLocalhostClustering();
            }
            else
            {
                var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
                    ?? throw new InvalidOperationException(
                        "Set the AZURE_STORAGE_CONNECTION_STRING environment variable for Orleans clustering.");

                clientBuilder.UseAzureStorageClustering(options =>
                    options.TableServiceClient = new TableServiceClient(connectionString));
            }
        });

        builder.Services.AddSingleton<IScriptValidationService, OrleansScriptValidationService>();

        return builder;
    }
}
