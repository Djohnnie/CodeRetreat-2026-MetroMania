FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/MetroMania.CleanupProcessor/MetroMania.CleanupProcessor.csproj src/MetroMania.CleanupProcessor/
RUN dotnet restore src/MetroMania.CleanupProcessor/MetroMania.CleanupProcessor.csproj

COPY src/MetroMania.CleanupProcessor/ src/MetroMania.CleanupProcessor/
RUN dotnet publish src/MetroMania.CleanupProcessor/MetroMania.CleanupProcessor.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MetroMania.CleanupProcessor.dll"]
