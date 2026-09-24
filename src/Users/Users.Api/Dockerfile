# Сборка из корня репозитория: docker build -f src/Users/Users.Api/Dockerfile .

# ── Stage 1: сборка ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — слой с restore кэшируется, пока не меняются зависимости
COPY src/Users/Users.Domain/Users.Domain.csproj                 src/Users/Users.Domain/
COPY src/Users/Users.Application/Users.Application.csproj       src/Users/Users.Application/
COPY src/Users/Users.Infrastructure/Users.Infrastructure.csproj src/Users/Users.Infrastructure/
COPY src/Users/Users.Api/Users.Api.csproj                       src/Users/Users.Api/
RUN dotnet restore src/Users/Users.Api/Users.Api.csproj

COPY src/Users/ src/Users/
RUN dotnet publish src/Users/Users.Api/Users.Api.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: запуск ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Users.Api.dll"]
