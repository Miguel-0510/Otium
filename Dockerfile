# Imagen de build - SDK de .NET 10.0
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Otium.csproj", "./"]
RUN dotnet restore "Otium.csproj" --disable-parallel
COPY . .
RUN dotnet publish "Otium.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=false \
    /p:TieredPGO=false \
    /p:OptimizationPreference=Size

# Imagen de runtime - ASP.NET Core 10.0
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV DOTNET_GCServer=0
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_EnableDiagnostics=0
ENV PORT=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Otium.dll"]
