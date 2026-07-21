FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Gateway/FlexFit.ApiGateway/FlexFit.ApiGateway.csproj", "src/Gateway/FlexFit.ApiGateway/"]
COPY ["src/BuildingBlocks/FlexFit.Contracts/FlexFit.Contracts.csproj", "src/BuildingBlocks/FlexFit.Contracts/"]
RUN dotnet restore "src/Gateway/FlexFit.ApiGateway/FlexFit.ApiGateway.csproj"

COPY src/ src/

WORKDIR "/src/src/Gateway/FlexFit.ApiGateway"
RUN dotnet build "FlexFit.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlexFit.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlexFit.ApiGateway.dll"]
