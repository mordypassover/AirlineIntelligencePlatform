FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Shared/Shared.csproj backend/Shared/
COPY backend/ReviewCollector/ReviewCollector.csproj backend/ReviewCollector/
RUN dotnet restore backend/ReviewCollector/ReviewCollector.csproj
COPY backend/ backend/
RUN dotnet publish backend/ReviewCollector/ReviewCollector.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet","ReviewCollector.dll"]
