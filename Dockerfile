# Multi-stage Dockerfile for AcxiomCRM Production Deployment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["AcxiomCRM.csproj", "./"]
RUN dotnet restore "AcxiomCRM.csproj"

# Copy all source files and publish release
COPY . .
RUN dotnet publish "AcxiomCRM.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AcxiomCRM.dll"]
