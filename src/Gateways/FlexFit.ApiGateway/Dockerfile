FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Gateways/FlexFit.ApiGateway/FlexFit.ApiGateway.csproj", "Gateways/FlexFit.ApiGateway/"]
RUN dotnet restore "Gateways/FlexFit.ApiGateway/FlexFit.ApiGateway.csproj"

COPY src/Gateways/ Gateways/

WORKDIR "/src/Gateways/FlexFit.ApiGateway"
RUN dotnet build "FlexFit.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlexFit.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlexFit.ApiGateway.dll"]
