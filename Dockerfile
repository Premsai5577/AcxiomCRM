# Multi-stage Dockerfile for P-CRM Production Deployment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["p-crm.csproj", "./"]
RUN dotnet restore "p-crm.csproj"

# Copy all source files and publish release
COPY . .
RUN dotnet publish "p-crm.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
USER root
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data && chmod -R 777 /app

# Expose ports
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "p-crm.dll"]
