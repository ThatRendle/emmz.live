FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/EmmzLive/EmmzLive.csproj src/EmmzLive/
RUN dotnet restore src/EmmzLive/EmmzLive.csproj

COPY src/EmmzLive/ src/EmmzLive/
RUN dotnet publish src/EmmzLive/EmmzLive.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Port binding is controlled at runtime by the PORT env var (defaulting to 8080) via UseUrls in
# Program.cs. EXPOSE is documentation only; Railway injects $PORT automatically.
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EmmzLive.dll"]
