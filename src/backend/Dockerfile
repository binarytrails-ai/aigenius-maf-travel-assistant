# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy ServiceDefaults first
COPY ["ContosoTravel.ServiceDefaults/ContosoTravel.ServiceDefaults.csproj", "ContosoTravel.ServiceDefaults/"]

# Copy backend project
COPY ["backend/ContosoTravelAgent.Host.csproj", "backend/"]

# Restore dependencies
RUN dotnet restore "backend/ContosoTravelAgent.Host.csproj"

# Copy everything else
COPY ["ContosoTravel.ServiceDefaults/", "ContosoTravel.ServiceDefaults/"]
COPY ["backend/", "backend/"]

# Build and publish
WORKDIR "/src/backend"
RUN dotnet publish "ContosoTravelAgent.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080

ENTRYPOINT ["dotnet", "ContosoTravelAgent.Host.dll"]
