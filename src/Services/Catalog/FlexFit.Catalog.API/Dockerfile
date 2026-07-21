# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/Services/Catalog/FlexFit.CatalogService/FlexFit.CatalogService.csproj", "src/Services/Catalog/FlexFit.CatalogService/"]
COPY ["src/BuildingBlocks/FlexFit.Contracts/FlexFit.Contracts.csproj", "src/BuildingBlocks/FlexFit.Contracts/"]
RUN dotnet restore "src/Services/Catalog/FlexFit.CatalogService/FlexFit.CatalogService.csproj"

# Copy all files and build
COPY src/ src/
WORKDIR "/src/src/Services/Catalog/FlexFit.CatalogService"
RUN dotnet build "FlexFit.CatalogService.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "FlexFit.CatalogService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose HTTP (REST) and HTTP/2 (gRPC) ports
EXPOSE 5002
EXPOSE 5003

# Setup Environment variables
ENV ASPNETCORE_URLS="http://+:5002;http://+:5003"
ENV ASPNETCORE_ENVIRONMENT="Production"

# Optional: Healthcheck inside the container using curl
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5002/health || exit 1

ENTRYPOINT ["dotnet", "FlexFit.CatalogService.dll"]
