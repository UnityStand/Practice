# Сборка из корня репозитория: docker build -f src/Bookings/Bookings.Api/Dockerfile .

# ── Stage 1: сборка ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — слой с restore кэшируется, пока не меняются зависимости
COPY src/Shared/EventApi.Contracts/EventApi.Contracts.csproj            src/Shared/EventApi.Contracts/
COPY src/Bookings/Bookings.Domain/Bookings.Domain.csproj                 src/Bookings/Bookings.Domain/
COPY src/Bookings/Bookings.Application/Bookings.Application.csproj       src/Bookings/Bookings.Application/
COPY src/Bookings/Bookings.Infrastructure/Bookings.Infrastructure.csproj src/Bookings/Bookings.Infrastructure/
COPY src/Bookings/Bookings.Api/Bookings.Api.csproj                       src/Bookings/Bookings.Api/
RUN dotnet restore src/Bookings/Bookings.Api/Bookings.Api.csproj

COPY src/Shared/ src/Shared/
COPY src/Bookings/ src/Bookings/
RUN dotnet publish src/Bookings/Bookings.Api/Bookings.Api.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: запуск ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bookings.Api.dll"]
