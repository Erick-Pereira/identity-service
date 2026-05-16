# Multi-stage — contexto de build: raiz da pasta identity-service (monorepo)
# ou repositório só do serviço (pastas Simcag.* no topo).
# Simcag.Shared: GitHub Packages (TOKEN + NUGET_GITHUB_OWNER) ou restore local no CI.

FROM mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim AS builder

WORKDIR /src

ARG BUILD_CONFIGURATION=Release
ARG TOKEN
ARG NUGET_GITHUB_OWNER

RUN if [ -n "${TOKEN:-}" ] && [ -n "${NUGET_GITHUB_OWNER:-}" ]; then \
    dotnet nuget add source "https://nuget.pkg.github.com/${NUGET_GITHUB_OWNER}/index.json" \
      --name github \
      --username "${NUGET_GITHUB_OWNER}" \
      --password "${TOKEN}" \
      --store-password-in-clear-text; \
  fi

COPY ["Simcag.IdentityService.Api/Simcag.IdentityService.Api.csproj", "Simcag.IdentityService.Api/"]
COPY ["Simcag.IdentityService.Application/Simcag.IdentityService.Application.csproj", "Simcag.IdentityService.Application/"]
COPY ["Simcag.IdentityService.Domain/Simcag.IdentityService.Domain.csproj", "Simcag.IdentityService.Domain/"]
COPY ["Simcag.IdentityService.Infrastructure/Simcag.IdentityService.Infrastructure.csproj", "Simcag.IdentityService.Infrastructure/"]

RUN dotnet restore "Simcag.IdentityService.Api/Simcag.IdentityService.Api.csproj"

COPY ["Simcag.IdentityService.Api/", "Simcag.IdentityService.Api/"]
COPY ["Simcag.IdentityService.Application/", "Simcag.IdentityService.Application/"]
COPY ["Simcag.IdentityService.Domain/", "Simcag.IdentityService.Domain/"]
COPY ["Simcag.IdentityService.Infrastructure/", "Simcag.IdentityService.Infrastructure/"]

RUN dotnet publish "Simcag.IdentityService.Api/Simcag.IdentityService.Api.csproj" \
    --configuration "${BUILD_CONFIGURATION}" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS runtime

WORKDIR /app

RUN groupadd -r appgroup && useradd -r -g appgroup appuser

COPY --from=builder /app/publish .

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && chown -R appuser:appgroup /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

USER appuser

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD sh -c 'p="${ASPNETCORE_HTTP_PORTS:-8080}"; p="${p%%,*}"; curl -fsS "http://localhost:${p}/health" >/dev/null || exit 1'

ENTRYPOINT ["dotnet", "Simcag.IdentityService.Api.dll"]
