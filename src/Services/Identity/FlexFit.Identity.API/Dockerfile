FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Identity/FlexFit.Identity.API/FlexFit.Identity.API.csproj", "src/Services/Identity/FlexFit.Identity.API/"]
COPY ["src/Services/Identity/FlexFit.Identity.Service/FlexFit.Identity.Service.csproj", "src/Services/Identity/FlexFit.Identity.Service/"]
COPY ["src/Services/Identity/FlexFit.Identity.Repository/FlexFit.Identity.Repository.csproj", "src/Services/Identity/FlexFit.Identity.Repository/"]
COPY ["src/BuildingBlocks/FlexFit.Caching/FlexFit.Caching.csproj", "src/BuildingBlocks/FlexFit.Caching/"]
COPY ["src/BuildingBlocks/FlexFit.RedisEventBus/FlexFit.RedisEventBus.csproj", "src/BuildingBlocks/FlexFit.RedisEventBus/"]
COPY ["src/BuildingBlocks/FlexFit.Contracts/FlexFit.Contracts.csproj", "src/BuildingBlocks/FlexFit.Contracts/"]
RUN dotnet restore "src/Services/Identity/FlexFit.Identity.API/FlexFit.Identity.API.csproj"
COPY src/ src/
WORKDIR "/src/src/Services/Identity/FlexFit.Identity.API"
RUN dotnet build "FlexFit.Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FlexFit.Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlexFit.Identity.API.dll"]
