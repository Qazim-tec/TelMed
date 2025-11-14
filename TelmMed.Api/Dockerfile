# ---- Base runtime (VS + Render) ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 8081

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy .csproj
COPY TelmMed.Api.csproj ./

# Restore
RUN dotnet restore

# Copy all source
COPY . .

# Build
RUN dotnet build "TelmMed.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ---- Publish ----
FROM build AS publish
RUN dotnet publish "TelmMed.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# ---- Final ----
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TelmMed.Api.dll"]