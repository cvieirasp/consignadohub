# ─────────────────────────────────────────────────────────────
# Stage 1: Build
# Build context must be the repo root to include building-blocks
# docker build -f src/services/NotificationService/Dockerfile -t consignadohub-notification .
# ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy central package management files first (layer cache optimisation)
COPY Directory.Build.props Directory.Packages.props ./

COPY src/building-blocks/ConsignadoHub.BuildingBlocks/ConsignadoHub.BuildingBlocks.csproj \
     src/building-blocks/ConsignadoHub.BuildingBlocks/

COPY src/building-blocks/ConsignadoHub.Contracts/ConsignadoHub.Contracts.csproj \
     src/building-blocks/ConsignadoHub.Contracts/

COPY src/services/NotificationService/src/NotificationService.Application/NotificationService.Application.csproj \
     src/services/NotificationService/src/NotificationService.Application/

COPY src/services/NotificationService/src/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj \
     src/services/NotificationService/src/NotificationService.Infrastructure/

COPY src/services/NotificationService/src/NotificationService.Worker/NotificationService.Worker.csproj \
     src/services/NotificationService/src/NotificationService.Worker/

RUN dotnet restore src/services/NotificationService/src/NotificationService.Worker/NotificationService.Worker.csproj

# Copy source code and publish
COPY src/building-blocks/ src/building-blocks/
COPY src/services/NotificationService/src/ src/services/NotificationService/src/

RUN dotnet publish src/services/NotificationService/src/NotificationService.Worker/NotificationService.Worker.csproj \
      --no-restore \
      --configuration Release \
      --output /app/publish

# ─────────────────────────────────────────────────────────────
# Stage 2: Runtime
# ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -sf http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "NotificationService.Worker.dll"]
