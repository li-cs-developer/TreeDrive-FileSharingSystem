FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/services/api-gateway/api-gateway.csproj backend/services/api-gateway/

RUN dotnet restore backend/services/api-gateway/api-gateway.csproj

# Copy all source
COPY . .

WORKDIR /src/backend/services/api-gateway
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "api-gateway.dll"]