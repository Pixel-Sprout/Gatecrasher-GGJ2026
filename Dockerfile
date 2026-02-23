# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

#build angular
FROM node:20-alpine AS ui-build
WORKDIR /src/ui

COPY Masquerade-UI/package*.json ./
RUN npm install
COPY Masquerade-UI/ ./
RUN npm run build --omit=dev

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["./Masquerade-API/Masquerade.csproj", "Masquerade-API/"]
RUN dotnet restore "./Masquerade-API/Masquerade.csproj"
COPY . .
COPY --from=ui-build /src/ui/dist/Masquerade-UI/browser /src/Masquerade-API/wwwroot/


WORKDIR "/src/Masquerade-API"
RUN dotnet build "./Masquerade.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/Masquerade-API"
RUN dotnet publish "./Masquerade.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Masquerade.dll"]
