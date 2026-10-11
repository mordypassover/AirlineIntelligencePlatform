FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Shared/Shared.csproj backend/Shared/
COPY backend/PriceCollector/PriceCollector.csproj backend/PriceCollector/
RUN dotnet restore backend/PriceCollector/PriceCollector.csproj
COPY backend/ backend/
RUN dotnet publish backend/PriceCollector/PriceCollector.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet","PriceCollector.dll"]
