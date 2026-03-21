# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy ServiceDefaults first
COPY ["ContosoTravel.ServiceDefaults/ContosoTravel.ServiceDefaults.csproj", "ContosoTravel.ServiceDefaults/"]

# Copy mcp project
COPY ["mcp/ContosoTravel.McpServer.csproj", "mcp/"]

# Restore dependencies
RUN dotnet restore "mcp/ContosoTravel.McpServer.csproj"

# Copy everything else
COPY ["ContosoTravel.ServiceDefaults/", "ContosoTravel.ServiceDefaults/"]
COPY ["mcp/", "mcp/"]

# Build and publish
WORKDIR "/src/mcp"
RUN dotnet publish "ContosoTravel.McpServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080

ENTRYPOINT ["dotnet", "ContosoTravel.McpServer.dll"]
