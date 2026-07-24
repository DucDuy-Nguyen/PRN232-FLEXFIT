FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Booking/FlexFit.Booking.API/FlexFit.Booking.API.csproj", "src/Services/Booking/FlexFit.Booking.API/"]
COPY ["src/Services/Booking/FlexFit.Booking.Service/FlexFit.Booking.Service.csproj", "src/Services/Booking/FlexFit.Booking.Service/"]
COPY ["src/Services/Booking/FlexFit.Booking.Repository/FlexFit.Booking.Repository.csproj", "src/Services/Booking/FlexFit.Booking.Repository/"]
COPY ["src/BuildingBlocks/FlexFit.Caching/FlexFit.Caching.csproj", "src/BuildingBlocks/FlexFit.Caching/"]
COPY ["src/BuildingBlocks/FlexFit.Contracts/FlexFit.Contracts.csproj", "src/BuildingBlocks/FlexFit.Contracts/"]
RUN dotnet restore "src/Services/Booking/FlexFit.Booking.API/FlexFit.Booking.API.csproj"
COPY src/ src/
WORKDIR "/src/src/Services/Booking/FlexFit.Booking.API"
RUN dotnet build "FlexFit.Booking.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FlexFit.Booking.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlexFit.Booking.API.dll"]
