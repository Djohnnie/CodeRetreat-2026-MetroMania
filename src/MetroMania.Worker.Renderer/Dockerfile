FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/MetroMania.Worker.Renderer/MetroMania.Worker.Renderer.csproj src/MetroMania.Worker.Renderer/
COPY src/MetroMania.Application/MetroMania.Application.csproj src/MetroMania.Application/
COPY src/MetroMania.Domain/MetroMania.Domain.csproj src/MetroMania.Domain/
COPY src/MetroMania.Engine/MetroMania.Engine.csproj src/MetroMania.Engine/
COPY src/MetroMania.Scripting/MetroMania.Scripting.csproj src/MetroMania.Scripting/
COPY src/MetroMania.Infrastructure.Sql/MetroMania.Infrastructure.Sql.csproj src/MetroMania.Infrastructure.Sql/
COPY src/MetroMania.Infrastructure.BlobStorage/MetroMania.Infrastructure.BlobStorage.csproj src/MetroMania.Infrastructure.BlobStorage/
RUN dotnet restore src/MetroMania.Worker.Renderer/MetroMania.Worker.Renderer.csproj

# Copy source and publish
COPY src/MetroMania.Worker.Renderer/ src/MetroMania.Worker.Renderer/
COPY src/MetroMania.Application/ src/MetroMania.Application/
COPY src/MetroMania.Domain/ src/MetroMania.Domain/
COPY src/MetroMania.Engine/ src/MetroMania.Engine/
COPY src/MetroMania.Scripting/ src/MetroMania.Scripting/
COPY src/MetroMania.Infrastructure.Sql/ src/MetroMania.Infrastructure.Sql/
COPY src/MetroMania.Infrastructure.BlobStorage/ src/MetroMania.Infrastructure.BlobStorage/
RUN dotnet publish src/MetroMania.Worker.Renderer/MetroMania.Worker.Renderer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

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

ENTRYPOINT ["dotnet", "MetroMania.Worker.Renderer.dll"]
