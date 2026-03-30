using Azure.Data.Tables;
using Orleans.Configuration;
using Orleans.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.UseOrleans(siloBuilder =>
{
    var azureStorageConnectionString = siloBuilder.Configuration.GetValue<string>("AZURE_STORAGE_CONNECTION_STRING");

#if DEBUG
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorageAsDefault();
#else
    siloBuilder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "metromania-orleans";
            options.ServiceId = "metromania-orleans";
        });

    siloBuilder.UseAzureStorageClustering(options =>
    {
        options.TableServiceClient = new TableServiceClient(azureStorageConnectionString);
    });
#endif

    siloBuilder.AddDashboard();
});

var app = builder.Build();

app.MapOrleansDashboard(routePrefix: "/dashboard");

app.Run();