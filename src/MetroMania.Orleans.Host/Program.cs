using Orleans.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorageAsDefault();
    siloBuilder.AddDashboard();
});

var app = builder.Build();

app.MapOrleansDashboard(routePrefix: "/dashboard");

app.Run();