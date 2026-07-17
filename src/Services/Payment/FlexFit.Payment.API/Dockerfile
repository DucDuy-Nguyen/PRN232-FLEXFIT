FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Payment/FlexFit.Payment.API/FlexFit.Payment.API.csproj", "src/Services/Payment/FlexFit.Payment.API/"]
COPY ["src/Services/Payment/FlexFit.Payment.Application/FlexFit.Payment.Application.csproj", "src/Services/Payment/FlexFit.Payment.Application/"]
COPY ["src/Services/Payment/FlexFit.Payment.Domain/FlexFit.Payment.Domain.csproj", "src/Services/Payment/FlexFit.Payment.Domain/"]
COPY ["src/Services/Payment/FlexFit.Payment.Contracts/FlexFit.Payment.Contracts.csproj", "src/Services/Payment/FlexFit.Payment.Contracts/"]
COPY ["src/Services/Payment/FlexFit.Payment.Infrastructure/FlexFit.Payment.Infrastructure.csproj", "src/Services/Payment/FlexFit.Payment.Infrastructure/"]

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
