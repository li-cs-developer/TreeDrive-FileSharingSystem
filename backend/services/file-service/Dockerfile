FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY backend/services/file-service/TreeDrive.FileService.csproj backend/services/file-service/
COPY backend/shared/TreeDrive.Core/TreeDrive.Core.csproj backend/shared/TreeDrive.Core/
COPY backend/shared/TreeDrive.Shared/TreeDrive.Shared.csproj backend/shared/TreeDrive.Shared/
COPY backend/infrastructure/TreeDrive.Infrastructure/TreeDrive.Infrastructure.csproj backend/infrastructure/TreeDrive.Infrastructure/
COPY backend/infrastructure/TreeDrive.FileStorage/TreeDrive.FileStorage.csproj backend/infrastructure/TreeDrive.FileStorage/

RUN dotnet restore backend/services/file-service/TreeDrive.FileService.csproj

# ✅ This copies ALL source code including the Controllers folder
COPY . .

WORKDIR /src/backend/services/file-service
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TreeDrive.FileService.dll"]