FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Shared/Shared.csproj backend/Shared/
COPY backend/SentimentService/SentimentService.csproj backend/SentimentService/
RUN dotnet restore backend/SentimentService/SentimentService.csproj
COPY backend/ backend/
RUN dotnet publish backend/SentimentService/SentimentService.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet","SentimentService.dll"]
