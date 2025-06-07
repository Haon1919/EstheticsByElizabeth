# Multi-stage build for Azure Functions .NET API

# Build stage - Use .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY API.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet build "API.csproj" -c Release -o /app/build
RUN dotnet publish "API.csproj" -c Release -o /app/publish --no-restore

# Runtime stage - Use Azure Functions runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS runtime

# Set the working directory
WORKDIR /home/site/wwwroot

# Copy the published application from build stage
COPY --from=build /app/publish .

# Expose the port that Azure Functions uses
EXPOSE 80

# Set environment variables for Azure Functions
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
