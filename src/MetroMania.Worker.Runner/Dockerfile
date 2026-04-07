FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/MetroMania.Worker.Runner/MetroMania.Worker.Runner.csproj src/MetroMania.Worker.Runner/
COPY src/MetroMania.Application/MetroMania.Application.csproj src/MetroMania.Application/
COPY src/MetroMania.Domain/MetroMania.Domain.csproj src/MetroMania.Domain/
COPY src/MetroMania.Engine/MetroMania.Engine.csproj src/MetroMania.Engine/
COPY src/MetroMania.Scripting/MetroMania.Scripting.csproj src/MetroMania.Scripting/
COPY src/MetroMania.Infrastructure.Sql/MetroMania.Infrastructure.Sql.csproj src/MetroMania.Infrastructure.Sql/
RUN dotnet restore src/MetroMania.Worker.Runner/MetroMania.Worker.Runner.csproj

# Copy source and publish
COPY src/MetroMania.Worker.Runner/ src/MetroMania.Worker.Runner/
COPY src/MetroMania.Application/ src/MetroMania.Application/
COPY src/MetroMania.Domain/ src/MetroMania.Domain/
COPY src/MetroMania.Engine/ src/MetroMania.Engine/
COPY src/MetroMania.Scripting/ src/MetroMania.Scripting/
COPY src/MetroMania.Infrastructure.Sql/ src/MetroMania.Infrastructure.Sql/
RUN dotnet publish src/MetroMania.Worker.Runner/MetroMania.Worker.Runner.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MetroMania.Worker.Runner.dll"]
