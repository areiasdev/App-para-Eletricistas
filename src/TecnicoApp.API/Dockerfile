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
USER appuser

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TecnicoApp.API.dll"]
