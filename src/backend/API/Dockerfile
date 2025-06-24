# Multi-stage build for Azure Functions .NET API

# Build stage - Use .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY API.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
# Debug: List files after copying source
RUN echo "=== DEBUG: Files in build stage /src ===" && \
    ls -la /src && \
    echo "=== DEBUG: Contents of local.settings.json in build stage ===" && \
    cat /src/local.settings.json || echo "local.settings.json not found in build stage" && \
    echo "=== END BUILD DEBUG ==="
# Explicitly ensure local.settings.json is copied
COPY local.settings.json .
RUN dotnet build "API.csproj" -c Release -o /app/build
RUN dotnet publish "API.csproj" -c Release -o /app/publish --no-restore

# Runtime stage - Use Azure Functions runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS runtime

# Install Python and pip
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Install Python MinIO client
RUN pip3 install minio --break-system-packages

# Set the working directory
WORKDIR /home/site/wwwroot

# Copy the published application from build stage
COPY --from=build /app/publish .

# Copy local.settings.json for development environment
COPY --from=build /src/local.settings.json .


# Debug: List files to verify what was copied
RUN echo "=== DEBUG: Files in /home/site/wwwroot ===" && \
    ls -la /home/site/wwwroot && \
    echo "=== END DEBUG ==="

# Expose the port that Azure Functions uses
EXPOSE 80

# Environment variables for Azure Functions with CORS configuration
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
    ASPNETCORE_ENVIRONMENT=Development \
    Host__CORS=*
