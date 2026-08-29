# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["PhotoShoot/PhotoShoot.csproj", "PhotoShoot/"]
RUN dotnet restore "PhotoShoot/PhotoShoot.csproj"

COPY . .
WORKDIR "/src/PhotoShoot"
RUN dotnet publish "PhotoShoot.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# libgomp1 is required by Magick.NET native libraries on Debian-based images
RUN apt-get update && apt-get install -y --no-install-recommends \
	libgomp1 \
	&& rm -rf /var/lib/apt/lists/*

# Create default data directories (overridable via volume mounts)
RUN mkdir -p /data/images /data/thumbnails /data/histograms /data/data-protection-keys

COPY --from=build /app/publish .

# Bind to HTTP only — HTTPS should be terminated by a reverse proxy
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Override appsettings.json folder paths with container-friendly defaults.
# These are overridable at runtime via environment variables or docker-compose.
ENV ImageMonitor__InputFolder=/data/images
ENV ImageMonitor__ThumbnailFolder=/data/thumbnails
ENV ImageMonitor__HistogramFolder=/data/histograms

EXPOSE 8080

ENTRYPOINT ["dotnet", "PhotoShoot.dll"]
