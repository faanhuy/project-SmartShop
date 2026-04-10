# ── Stage 1: Restore ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /app

# Copy only csproj files first for layer caching
COPY src/SmartShop.Domain/SmartShop.Domain.csproj           src/SmartShop.Domain/
COPY src/SmartShop.Application/SmartShop.Application.csproj src/SmartShop.Application/
COPY src/SmartShop.Infrastructure/SmartShop.Infrastructure.csproj src/SmartShop.Infrastructure/
COPY src/SmartShop.WebAPI/SmartShop.WebAPI.csproj            src/SmartShop.WebAPI/

RUN dotnet restore src/SmartShop.WebAPI/SmartShop.WebAPI.csproj

# ── Stage 2: Build ────────────────────────────────────────────────────────────
FROM restore AS build
COPY src/ src/
RUN dotnet build src/SmartShop.WebAPI/SmartShop.WebAPI.csproj -c Release --no-restore

# ── Stage 3: Publish ──────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish src/SmartShop.WebAPI/SmartShop.WebAPI.csproj -c Release --no-build -o /app/publish

# ── Stage 4: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SmartShop.WebAPI.dll"]
