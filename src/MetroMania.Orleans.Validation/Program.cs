using Azure.Data.Tables;
using Orleans.Dashboard;

var builder = WebApplication.CreateBuilder(args);

#if ASPIRE
builder.AddServiceDefaults();
#endif

builder.UseOrleans(siloBuilder =>
{
#if ASPIRE
    siloBuilder.UseLocalhostClustering();
#else
    var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
        ?? throw new InvalidOperationException(
            "Set the AZURE_STORAGE_CONNECTION_STRING environment variable for Orleans clustering.");

    siloBuilder.UseAzureStorageClustering(options =>
        options.TableServiceClient = new TableServiceClient(connectionString));
#endif

    siloBuilder.AddDashboard();
});

var app = builder.Build();

app.MapOrleansDashboard(routePrefix: "/dashboard");

#if ASPIRE
app.MapDefaultEndpoints();
#endif

app.Run();
