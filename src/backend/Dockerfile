# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ContosoTravelAgent.Host.csproj", "./"]
RUN dotnet restore "ContosoTravelAgent.Host.csproj"

# Copy everything else and build
COPY . .
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
