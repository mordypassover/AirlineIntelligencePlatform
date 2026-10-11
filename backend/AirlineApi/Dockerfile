FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Shared/Shared.csproj backend/Shared/
COPY backend/AirlineApi/AirlineApi.csproj backend/AirlineApi/
RUN dotnet restore backend/AirlineApi/AirlineApi.csproj
COPY backend/ backend/
RUN dotnet publish backend/AirlineApi/AirlineApi.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet","AirlineApi.dll"]
