FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Payment/FlexFit.Payment.API/FlexFit.Payment.API.csproj", "src/Services/Payment/FlexFit.Payment.API/"]
COPY ["src/Services/Payment/FlexFit.Payment.Service/FlexFit.Payment.Service.csproj", "src/Services/Payment/FlexFit.Payment.Service/"]
COPY ["src/Services/Payment/FlexFit.Payment.Repository/FlexFit.Payment.Repository.csproj", "src/Services/Payment/FlexFit.Payment.Repository/"]

RUN dotnet restore "src/Services/Payment/FlexFit.Payment.API/FlexFit.Payment.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Payment/FlexFit.Payment.API"
RUN dotnet build "FlexFit.Payment.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlexFit.Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlexFit.Payment.API.dll"]
