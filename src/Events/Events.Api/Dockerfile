# Сборка из корня репозитория: docker build -f src/Events/Events.Api/Dockerfile .

# ── Stage 1: сборка ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — слой с restore кэшируется, пока не меняются зависимости
COPY src/Shared/EventApi.Contracts/EventApi.Contracts.csproj      src/Shared/EventApi.Contracts/
COPY src/Events/Events.Domain/Events.Domain.csproj                 src/Events/Events.Domain/
COPY src/Events/Events.Application/Events.Application.csproj       src/Events/Events.Application/
COPY src/Events/Events.Infrastructure/Events.Infrastructure.csproj src/Events/Events.Infrastructure/
COPY src/Events/Events.Api/Events.Api.csproj                       src/Events/Events.Api/
RUN dotnet restore src/Events/Events.Api/Events.Api.csproj

COPY src/Shared/ src/Shared/
COPY src/Events/ src/Events/
RUN dotnet publish src/Events/Events.Api/Events.Api.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: запуск ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Events.Api.dll"]
