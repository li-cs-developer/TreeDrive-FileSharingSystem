# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY TreeDrive.slnx .
COPY TreeDrive.API/*.csproj ./TreeDrive.API/
COPY TreeDrive.Core/*.csproj ./TreeDrive.Core/
COPY TreeDrive.Infrastructure/*.csproj ./TreeDrive.Infrastructure/
COPY TreeDrive.FileService/*.csproj ./TreeDrive.FileService/
COPY TreeDrive.Shared/*.csproj ./TreeDrive.Shared/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY TreeDrive.API/. ./TreeDrive.API/
COPY TreeDrive.Core/. ./TreeDrive.Core/
COPY TreeDrive.Infrastructure/. ./TreeDrive.Infrastructure/
COPY TreeDrive.FileService/. ./TreeDrive.FileService/
COPY TreeDrive.Shared/. ./TreeDrive.Shared/

# Build and publish
WORKDIR /src/TreeDrive.API
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Create directory for uploaded files
RUN mkdir -p /app/server_files

ENTRYPOINT ["dotnet", "TreeDrive.API.dll"]