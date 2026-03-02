# ─────────────────────────────────────────────────────────────
# Stage 1: Build
# Build context must be the repo root to include building-blocks
# docker build -f src/services/ProposalService/Dockerfile -t consignadohub-proposal .
# ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy central package management files first (layer cache optimisation)
COPY Directory.Build.props Directory.Packages.props ./

# Copy only .csproj files to restore dependencies before copying source code.
# This maximises layer caching: a source-only change won't invalidate the restore layer.
COPY src/building-blocks/ConsignadoHub.BuildingBlocks/ConsignadoHub.BuildingBlocks.csproj \
     src/building-blocks/ConsignadoHub.BuildingBlocks/

COPY src/services/ProposalService/src/ProposalService.Domain/ProposalService.Domain.csproj \
     src/services/ProposalService/src/ProposalService.Domain/

COPY src/services/ProposalService/src/ProposalService.Application/ProposalService.Application.csproj \
     src/services/ProposalService/src/ProposalService.Application/

COPY src/services/ProposalService/src/ProposalService.Infrastructure/ProposalService.Infrastructure.csproj \
     src/services/ProposalService/src/ProposalService.Infrastructure/

COPY src/services/ProposalService/src/ProposalService.Api/ProposalService.Api.csproj \
     src/services/ProposalService/src/ProposalService.Api/

RUN dotnet restore src/services/ProposalService/src/ProposalService.Api/ProposalService.Api.csproj

# Copy source code and publish
COPY src/building-blocks/ src/building-blocks/
COPY src/services/ProposalService/src/ src/services/ProposalService/src/

RUN dotnet publish src/services/ProposalService/src/ProposalService.Api/ProposalService.Api.csproj \
      --no-restore \
      --configuration Release \
      --output /app/publish

# ─────────────────────────────────────────────────────────────
# Stage 2: Runtime
# ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install curl for the HEALTHCHECK probe
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Run as non-root user (built into the aspnet image as uid/gid 1654)
USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -sf http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "ProposalService.Api.dll"]
