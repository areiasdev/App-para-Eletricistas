# ── Stage 1: build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching on restore
COPY ["src/TecnicoApp.API/TecnicoApp.API.csproj",                   "src/TecnicoApp.API/"]
COPY ["src/TecnicoApp.Application/TecnicoApp.Application.csproj",   "src/TecnicoApp.Application/"]
COPY ["src/TecnicoApp.Infrastructure/TecnicoApp.Infrastructure.csproj", "src/TecnicoApp.Infrastructure/"]
COPY ["src/TecnicoApp.Domain/TecnicoApp.Domain.csproj",             "src/TecnicoApp.Domain/"]
RUN dotnet restore "src/TecnicoApp.API/TecnicoApp.API.csproj"

# Copy source and publish
COPY . .
RUN dotnet publish "src/TecnicoApp.API/TecnicoApp.API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    -p:UseAppHost=false

# ── Stage 2: runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

# Non-root user for security
RUN groupadd --system --gid 1001 appgroup && \
    useradd --system --uid 1001 --gid appgroup appuser

COPY --from=build --chown=appuser:appgroup /app/publish .
# Uploaded logos live under wwwroot/uploads, which docker-compose.prod.yml mounts as a volume
# so they survive `up --build`. Create it owned by appuser: a volume mounted on a path missing
# from the image is created root-owned and the non-root process couldn't write to it.
RUN mkdir -p /app/wwwroot/uploads && chown -R appuser:appgroup /app/wwwroot
USER appuser

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TecnicoApp.API.dll"]
