FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Otium.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV ConnectionStrings__DefaultConnection="Data Source=/data/app.db"
ENV DataProtection__KeysPath=/data/keys

RUN mkdir -p /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "Otium.dll"]
