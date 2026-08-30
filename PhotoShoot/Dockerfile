# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PhotoShoot/PhotoShoot.csproj", "PhotoShoot/"]
RUN dotnet restore "./PhotoShoot.csproj"
COPY . .
WORKDIR "/src/PhotoShoot"
RUN dotnet build "./PhotoShoot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PhotoShoot.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-build /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# libgomp1 is required by Magick.NET native libraries on Debian-based images
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# Create default data directories (overridable via volume mounts)
RUN mkdir -p /data/images /data/thumbnails /data/histograms /data/data-protection-keys

COPY --from=publish /app/publish .

# Bind to HTTP only — HTTPS should be terminated by a reverse proxy
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Override appsettings.json folder paths with container-friendly defaults.
# These are overridable at runtime via environment variables or docker-compose.
ENV ImageMonitor__InputFolder=/data/images
ENV ImageMonitor__ThumbnailFolder=/data/thumbnails
ENV ImageMonitor__HistogramFolder=/data/histograms
ENV PHOTOSHOOT_DATA_PROTECTION_KEYS=/data/data-protection-keys

ENTRYPOINT ["dotnet", "PhotoShoot.dll"]
