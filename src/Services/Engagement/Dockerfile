FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files for restore
COPY ["src/Services/Engagement/FlexFit.Engagement.API/FlexFit.Engagement.API.csproj", "Services/Engagement/FlexFit.Engagement.API/"]
COPY ["src/Services/Engagement/FlexFit.Engagement.Service/FlexFit.Engagement.Service.csproj", "Services/Engagement/FlexFit.Engagement.Service/"]
COPY ["src/Services/Engagement/FlexFit.Engagement.Repository/FlexFit.Engagement.Repository.csproj", "Services/Engagement/FlexFit.Engagement.Repository/"]
COPY ["src/BuildingBlocks/FlexFit.Contracts/FlexFit.Contracts.csproj", "BuildingBlocks/FlexFit.Contracts/"]
COPY ["src/Services/Recommendation/FlexFit.Recommendation.Grpc/FlexFit.Recommendation.Grpc.csproj", "Services/Recommendation/FlexFit.Recommendation.Grpc/"]

# Restore
RUN dotnet restore "Services/Engagement/FlexFit.Engagement.API/FlexFit.Engagement.API.csproj"

# Copy the rest of the source code
COPY src/Services/Engagement/ Services/Engagement/
COPY src/BuildingBlocks/ BuildingBlocks/
COPY src/Services/Recommendation/ Services/Recommendation/

# Build
WORKDIR "/src/Services/Engagement/FlexFit.Engagement.API"
RUN dotnet build "FlexFit.Engagement.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlexFit.Engagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlexFit.Engagement.API.dll"]
