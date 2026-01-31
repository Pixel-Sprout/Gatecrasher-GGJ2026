# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

#build angular
FROM node:20-alpine AS ui-build
WORKDIR /src/ui

COPY Masquerade-Angular/Masquerade-Client/package*.json ./
RUN npm install
COPY Masquerade-Angular/Masquerade-Client/ ./
RUN npm run build --omit=dev

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["./Masquerade-GGJ-2026/Masquerade-GGJ-2026.csproj", "Masquerade-GGJ-2026/"]
RUN dotnet restore "./Masquerade-GGJ-2026/Masquerade-GGJ-2026.csproj"
COPY . .
COPY --from=ui-build /src/ui/dist/Masquerade-Client/browser /src/Masquerade-GGJ-2026/wwwroot/


WORKDIR "/src/Masquerade-GGJ-2026"
RUN dotnet build "./Masquerade-GGJ-2026.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/Masquerade-GGJ-2026"
RUN dotnet publish "./Masquerade-GGJ-2026.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Masquerade-GGJ-2026.dll"]
