# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/Services/Catalog/FlexFit.Catalog.API/FlexFit.Catalog.API.csproj", "src/Services/Catalog/FlexFit.Catalog.API/"]
COPY ["src/Services/Catalog/FlexFit.Catalog.Service/FlexFit.Catalog.Service.csproj", "src/Services/Catalog/FlexFit.Catalog.Service/"]
COPY ["src/Services/Catalog/FlexFit.Catalog.Repository/FlexFit.Catalog.Repository.csproj", "src/Services/Catalog/FlexFit.Catalog.Repository/"]
COPY ["src/BuildingBlocks/FlexFit.Contracts/FlexFit.Contracts.csproj", "src/BuildingBlocks/FlexFit.Contracts/"]
RUN dotnet restore "src/Services/Catalog/FlexFit.Catalog.API/FlexFit.Catalog.API.csproj"

# Copy all files and build
COPY src/ src/
WORKDIR "/src/src/Services/Catalog/FlexFit.Catalog.API"
RUN dotnet build "FlexFit.Catalog.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "FlexFit.Catalog.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "FlexFit.Catalog.API.dll"]
