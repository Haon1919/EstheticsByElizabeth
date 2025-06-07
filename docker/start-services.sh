#!/bin/bash

# Build and start all services
echo "🐳 Building and starting Docker services..."

# Navigate to the docker directory
cd "$(dirname "$0")"

# Build and start the services
docker-compose up --build -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 10

# Check service status
echo "📊 Service Status:"
docker-compose ps

# Show logs for troubleshooting
echo "📝 Recent API logs:"
docker-compose logs --tail=20 api

echo "✅ Services are running!"
echo "🌐 API is available at: http://localhost:7071"
echo "🗄️  PostgreSQL is available at: localhost:5432"
echo ""
echo "To view logs: docker-compose logs -f [service-name]"
echo "To stop services: docker-compose down"
echo "To stop and remove volumes: docker-compose down -v"
