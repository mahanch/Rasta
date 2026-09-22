# Stage 1: Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build & Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy NuGet configuration and project files for layer caching
COPY ["nuget.config", "./"]
COPY ["src/Shop/Shop.Api/Shop.Api.csproj", "src/Shop/Shop.Api/"]
COPY ["src/Shop/Shop.Application/Shop.Application.csproj", "src/Shop/Shop.Application/"]
COPY ["src/Shop/Shop.Domain/Shop.Domain.csproj", "src/Shop/Shop.Domain/"]
COPY ["src/Shop/Shop.Infrastructure/Shop.Infrastructure.csproj", "src/Shop/Shop.Infrastructure/"]
COPY ["src/Aspire/Shop.ServiceDefaults/Shop.ServiceDefaults.csproj", "src/Aspire/Shop.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/Shop/Shop.Api/Shop.Api.csproj"

# Copy source code
COPY ["src/Shop/", "src/Shop/"]
COPY ["src/Aspire/Shop.ServiceDefaults/", "src/Aspire/Shop.ServiceDefaults/"]

# Build application
WORKDIR "/src/src/Shop/Shop.Api"
RUN dotnet build "Shop.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Shop.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Shop.Api.dll"]
