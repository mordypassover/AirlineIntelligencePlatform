FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Shared/Shared.csproj backend/Shared/
COPY backend/FlightCollector/FlightCollector.csproj backend/FlightCollector/
RUN dotnet restore backend/FlightCollector/FlightCollector.csproj
COPY backend/ backend/
RUN dotnet publish backend/FlightCollector/FlightCollector.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet","FlightCollector.dll"]
