# Dockerfile for the water controller API.
# Build context is the water-controller-server/ directory:
#   docker build -f Api/Dockerfile -t water-controller-api .
# Run (must set the connection string to point at the db service):
#   docker run -d --name water-controller-api -p 5000:5000 \
#     -e ConnectionStrings__WaterController="Host=db;Port=5432;Database=water_controller;Username=postgres;Password=postgres" \
#     water-controller-api

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore using project files only so dependencies layer is cached.
COPY Api/Api.csproj Api/
COPY Model/Model.csproj Model/
COPY Service/Service.csproj Service/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
RUN dotnet restore Api/Api.csproj

# Copy the remaining sources and publish.
COPY . .
RUN dotnet publish Api/Api.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Api.dll"]
