FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/EmmzLive/EmmzLive.csproj src/EmmzLive/
RUN dotnet restore src/EmmzLive/EmmzLive.csproj

COPY src/EmmzLive/ src/EmmzLive/
RUN dotnet publish src/EmmzLive/EmmzLive.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Railway routes external traffic to port 8080; set $PORT in Railway service settings if overriding.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EmmzLive.dll"]
