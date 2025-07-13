# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["KelimeBulma/KelimeBulma.csproj", "KelimeBulma/"]
RUN dotnet restore "./KelimeBulma/KelimeBulma.csproj"
COPY . . 
WORKDIR "/src/KelimeBulma"
RUN dotnet build "./KelimeBulma.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish image
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./KelimeBulma.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Set ASP.NET Core to listen on all IP addresses (0.0.0.0) at port 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "KelimeBulma.dll"]
