FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Shared/Shared.csproj backend/Shared/
COPY backend/AnalyticsService/AnalyticsService.csproj backend/AnalyticsService/
RUN dotnet restore backend/AnalyticsService/AnalyticsService.csproj
COPY backend/ backend/
RUN dotnet publish backend/AnalyticsService/AnalyticsService.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet","AnalyticsService.dll"]
