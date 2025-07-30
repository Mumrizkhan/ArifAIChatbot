FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AnalyticsService/AnalyticsService.csproj", "src/AnalyticsService/"]
COPY ["src/Shared/Domain/Shared.Domain.csproj", "src/Shared/Domain/"]
COPY ["src/Shared/Application/Shared.Application.csproj", "src/Shared/Application/"]
COPY ["src/Shared/Infrastructure/Shared.Infrastructure.csproj", "src/Shared/Infrastructure/"]
RUN dotnet restore "src/AnalyticsService/AnalyticsService.csproj"
COPY . .
WORKDIR "/src/src/AnalyticsService"
RUN dotnet build "AnalyticsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AnalyticsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AnalyticsService.dll"]
