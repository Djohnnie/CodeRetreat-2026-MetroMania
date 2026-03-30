FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/MetroMania.Worker/MetroMania.Worker.csproj src/MetroMania.Worker/
COPY src/MetroMania.Application/MetroMania.Application.csproj src/MetroMania.Application/
COPY src/MetroMania.Domain/MetroMania.Domain.csproj src/MetroMania.Domain/
COPY src/MetroMania.Engine/MetroMania.Engine.csproj src/MetroMania.Engine/
COPY src/MetroMania.Infrastructure.Sql/MetroMania.Infrastructure.Sql.csproj src/MetroMania.Infrastructure.Sql/
COPY src/MetroMania.Infrastructure.Orleans/MetroMania.Infrastructure.Orleans.csproj src/MetroMania.Infrastructure.Orleans/
COPY src/MetroMania.Infrastructure.ServiceBus/MetroMania.Infrastructure.ServiceBus.csproj src/MetroMania.Infrastructure.ServiceBus/
COPY src/MetroMania.Orleans.Contracts/MetroMania.Orleans.Contracts.csproj src/MetroMania.Orleans.Contracts/
RUN dotnet restore src/MetroMania.Worker/MetroMania.Worker.csproj

# Copy source and publish
COPY src/MetroMania.Worker/ src/MetroMania.Worker/
COPY src/MetroMania.Application/ src/MetroMania.Application/
COPY src/MetroMania.Domain/ src/MetroMania.Domain/
COPY src/MetroMania.Engine/ src/MetroMania.Engine/
COPY src/MetroMania.Infrastructure.Sql/ src/MetroMania.Infrastructure.Sql/
COPY src/MetroMania.Infrastructure.Orleans/ src/MetroMania.Infrastructure.Orleans/
COPY src/MetroMania.Infrastructure.ServiceBus/ src/MetroMania.Infrastructure.ServiceBus/
COPY src/MetroMania.Orleans.Contracts/ src/MetroMania.Orleans.Contracts/
RUN dotnet publish src/MetroMania.Worker/MetroMania.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MetroMania.Worker.dll"]
