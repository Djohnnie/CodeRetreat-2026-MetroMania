FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/MetroMania.Web/MetroMania.Web.csproj src/MetroMania.Web/
COPY src/MetroMania.Application/MetroMania.Application.csproj src/MetroMania.Application/
COPY src/MetroMania.Domain/MetroMania.Domain.csproj src/MetroMania.Domain/
COPY src/MetroMania.Engine/MetroMania.Engine.csproj src/MetroMania.Engine/
COPY src/MetroMania.Scripting/MetroMania.Scripting.csproj src/MetroMania.Scripting/
RUN dotnet restore src/MetroMania.Web/MetroMania.Web.csproj -p:UseAspire=false

# Copy source and publish
COPY src/MetroMania.Web/ src/MetroMania.Web/
COPY src/MetroMania.Application/ src/MetroMania.Application/
COPY src/MetroMania.Domain/ src/MetroMania.Domain/
COPY src/MetroMania.Engine/ src/MetroMania.Engine/
COPY src/MetroMania.Scripting/ src/MetroMania.Scripting/
RUN dotnet publish src/MetroMania.Web/MetroMania.Web.csproj -c Release -o /app/publish --no-restore -p:UseAspire=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY resources/ /app/resources/
EXPOSE 8080
ENTRYPOINT ["dotnet", "MetroMania.Web.dll"]