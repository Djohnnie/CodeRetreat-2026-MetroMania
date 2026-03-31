FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/MetroMania.Orleans.Host/MetroMania.Orleans.Host.csproj src/MetroMania.Orleans.Host/
COPY src/MetroMania.Orleans.Contracts/MetroMania.Orleans.Contracts.csproj src/MetroMania.Orleans.Contracts/
COPY src/MetroMania.Engine/MetroMania.Engine.csproj src/MetroMania.Engine/
COPY src/MetroMania.Domain/MetroMania.Domain.csproj src/MetroMania.Domain/
RUN dotnet restore src/MetroMania.Orleans.Host/MetroMania.Orleans.Host.csproj

# Copy source and publish
COPY src/MetroMania.Orleans.Host/ src/MetroMania.Orleans.Host/
COPY src/MetroMania.Orleans.Contracts/ src/MetroMania.Orleans.Contracts/
COPY src/MetroMania.Engine/ src/MetroMania.Engine/
COPY src/MetroMania.Domain/ src/MetroMania.Domain/
RUN dotnet publish src/MetroMania.Orleans.Host/MetroMania.Orleans.Host.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# SkiaSharp native dependencies for game engine rendering
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    libpng16-16 \
    libglib2.0-0 \
    libharfbuzz0b \
    libexpat1 \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Copy SVG tile resources for the game renderer
COPY resources/ /app/resources/

EXPOSE 8080
ENTRYPOINT ["dotnet", "MetroMania.Orleans.Host.dll"]
