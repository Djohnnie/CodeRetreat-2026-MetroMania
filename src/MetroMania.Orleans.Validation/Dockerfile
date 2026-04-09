FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/MetroMania.Orleans.Validation/MetroMania.Orleans.Validation.csproj src/MetroMania.Orleans.Validation/
COPY src/MetroMania.Orleans.Client/MetroMania.Orleans.Client.csproj src/MetroMania.Orleans.Client/
COPY src/MetroMania.Application/MetroMania.Application.csproj src/MetroMania.Application/
COPY src/MetroMania.Domain/MetroMania.Domain.csproj src/MetroMania.Domain/
COPY src/MetroMania.Engine/MetroMania.Engine.csproj src/MetroMania.Engine/
COPY src/MetroMania.Scripting/MetroMania.Scripting.csproj src/MetroMania.Scripting/
RUN dotnet restore src/MetroMania.Orleans.Validation/MetroMania.Orleans.Validation.csproj -p:UseAspire=false

# Copy source and publish
COPY src/MetroMania.Orleans.Validation/ src/MetroMania.Orleans.Validation/
COPY src/MetroMania.Orleans.Client/ src/MetroMania.Orleans.Client/
COPY src/MetroMania.Application/ src/MetroMania.Application/
COPY src/MetroMania.Domain/ src/MetroMania.Domain/
COPY src/MetroMania.Engine/ src/MetroMania.Engine/
COPY src/MetroMania.Scripting/ src/MetroMania.Scripting/
RUN dotnet publish src/MetroMania.Orleans.Validation/MetroMania.Orleans.Validation.csproj -c Release -o /app/publish --no-restore -p:UseAspire=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MetroMania.Orleans.Validation.dll"]
