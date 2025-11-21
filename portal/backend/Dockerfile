# Primus SaaS Portal - Backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["portal/backend/Primus.Portal.Api/Primus.Portal.Api.csproj", "portal/backend/Primus.Portal.Api/"]
RUN dotnet restore "portal/backend/Primus.Portal.Api/Primus.Portal.Api.csproj"

# Copy everything else and build
COPY portal/backend/ portal/backend/
WORKDIR "/src/portal/backend/Primus.Portal.Api"
RUN dotnet build "Primus.Portal.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Primus.Portal.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Primus.Portal.Api.dll"]
