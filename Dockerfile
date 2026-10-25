# Imagen de build - SDK de .NET 10.0
# NOTA: En producción, fijar las imágenes por digest (sha256) para reproducibilidad:
# FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:XXXX... AS build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Otium.csproj ./
RUN dotnet restore
RUN dotnet nuget locals all --clear
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false /p:PublishReadyToRun=false /p:TieredPGO=false

# Imagen de runtime - ASP.NET Core 10.0
# NOTA: En producción, fijar las imágenes por digest (sha256):
# FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:XXXX...
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV ConnectionStrings__DefaultConnection="Data Source=/data/app.db"
ENV DataProtection__KeysPath=/data/keys

# Runtime de .NET ultra liviano para entornos con RAM limitada (ej. Render free tier 512 MB).
ENV DOTNET_GCServer=0
ENV DOTNET_GCHPT=0
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV GCHeapHardLimit=0x10000000
ENV DOTNET_EnableDiagnostics=0

RUN mkdir -p /data

# Usuario no root para ejecutar la aplicación.
RUN groupadd -r otium && useradd -r -g otium -m -d /home/otium otium
RUN chown -R otium:otium /app /data
USER otium

# Cambiar a usuario root para instalar curl (necesita permisos de escritura en /var/lib/apt/lists).
USER root

# Curl para el healthcheck; se limpian las listas de apt en la misma capa.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Volver al usuario no root para ejecutar la aplicación.
USER otium

VOLUME /data

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Otium.dll"]
