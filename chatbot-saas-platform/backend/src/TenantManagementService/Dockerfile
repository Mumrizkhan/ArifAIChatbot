FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TenantManagementService/TenantManagementService.csproj", "TenantManagementService/"]
COPY ["Shared/Domain/Shared.Domain.csproj", "Shared/Domain/"]
COPY ["Shared/Application/Shared.Application.csproj", "Shared/Application/"]
COPY ["Shared/Infrastructure/Shared.Infrastructure.csproj", "Shared/Infrastructure/"]
RUN dotnet restore "TenantManagementService/TenantManagementService.csproj"
COPY . .
WORKDIR "/src/TenantManagementService"
RUN dotnet build "TenantManagementService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TenantManagementService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TenantManagementService.dll"]
