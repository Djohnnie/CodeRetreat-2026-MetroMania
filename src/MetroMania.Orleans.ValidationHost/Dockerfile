FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/MetroMania.Orleans.ValidationHost/MetroMania.Orleans.ValidationHost.csproj src/MetroMania.Orleans.ValidationHost/
COPY src/MetroMania.Orleans.Contracts/MetroMania.Orleans.Contracts.csproj src/MetroMania.Orleans.Contracts/
COPY src/MetroMania.Engine/MetroMania.Engine.csproj src/MetroMania.Engine/
COPY src/MetroMania.Domain/MetroMania.Domain.csproj src/MetroMania.Domain/
RUN dotnet restore src/MetroMania.Orleans.ValidationHost/MetroMania.Orleans.ValidationHost.csproj

# Copy source and publish
COPY src/MetroMania.Orleans.ValidationHost/ src/MetroMania.Orleans.ValidationHost/
COPY src/MetroMania.Orleans.Contracts/ src/MetroMania.Orleans.Contracts/
COPY src/MetroMania.Engine/ src/MetroMania.Engine/
COPY src/MetroMania.Domain/ src/MetroMania.Domain/
RUN dotnet publish src/MetroMania.Orleans.ValidationHost/MetroMania.Orleans.ValidationHost.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# SkiaSharp native dependencies for game engine rendering
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MetroMania.Orleans.ValidationHost.dll"]
