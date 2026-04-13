# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["Restaurante.API/Restaurante.API.csproj", "Restaurante.API/"]
COPY ["Restaurante.Application/Restaurante.Application.csproj", "Restaurante.Application/"]
COPY ["Restaurante.Domain/Restaurante.Domain.csproj", "Restaurante.Domain/"]
COPY ["Restaurante.Infrastructure/Restaurante.Infrastructure.csproj", "Restaurante.Infrastructure/"]

RUN dotnet restore "Restaurante.API/Restaurante.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Restaurante.API"
RUN dotnet build "Restaurante.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "Restaurante.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Restaurante.API.dll"]
