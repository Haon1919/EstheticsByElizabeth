#!/bin/bash

# Stop and clean up Docker services
echo "🛑 Stopping Docker services..."

# Navigate to the docker directory
cd "$(dirname "$0")"

# Stop and remove containers
docker-compose down

echo "🧹 Cleaning up..."

# Optional: Remove volumes (uncomment if you want to reset database)
# docker-compose down -v

# Optional: Remove images (uncomment if you want to clean up completely)
# docker-compose down --rmi local

echo "✅ Services stopped successfully!"
