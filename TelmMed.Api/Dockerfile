# -------------------------------------------------
# 1. Base runtime (used for VS fast mode & final)
# -------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 8081

# -------------------------------------------------
# 2. Build stage
# -------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file (CORRECT PATH)
COPY ["TelmMed.Api.csproj", "TelmMed.Api/"]

# Restore
RUN dotnet restore "./TelmMed.Api/TelmMed.Api.csproj"

# Copy source
COPY . .

# Build
WORKDIR "/src/TelmMed.Api"
RUN dotnet build "./TelmMed.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# -------------------------------------------------
# 3. Publish stage
# -------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TelmMed.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# -------------------------------------------------
# 4. Final runtime image
# -------------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TelmMed.Api.dll"]