FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY backend/services/auth-service/TreeDrive.AuthService.csproj backend/services/auth-service/
COPY backend/shared/TreeDrive.Core/TreeDrive.Core.csproj backend/shared/TreeDrive.Core/
COPY backend/shared/TreeDrive.Shared/TreeDrive.Shared.csproj backend/shared/TreeDrive.Shared/
COPY backend/infrastructure/TreeDrive.Infrastructure/TreeDrive.Infrastructure.csproj backend/infrastructure/TreeDrive.Infrastructure/

RUN dotnet restore backend/services/auth-service/TreeDrive.AuthService.csproj

# ✅ Copy ALL source code
COPY . .

WORKDIR /src/backend/services/auth-service
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# ✅ Set the port to 5001
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001

# ✅ Make sure the Controllers folder is copied
RUN ls -la /app/

ENTRYPOINT ["dotnet", "TreeDrive.AuthService.dll"]