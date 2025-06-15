#!/bin/bash

# 🚀 Start EstheticsByElizabeth Services with MinIO 🚀
# This script starts PostgreSQL, MinIO, and the API services

echo "🚀 Starting EstheticsByElizabeth services with MinIO storage..."

# Change to the docker directory
cd "$(dirname "$0")"

# Start the services
echo "📦 Building and starting Docker containers..."
docker-compose up -d --build

# Wait for services to be ready
echo "⏳ Waiting for services to initialize..."
sleep 10

# Check service status
echo "🔍 Checking service status..."
docker-compose ps

echo ""
echo "✅ Services are starting up!"
echo ""
echo "📊 Service URLs:"
echo "   🌐 API: http://localhost"
echo "   🗄️ PostgreSQL: localhost:5432"
echo "   🪣 MinIO Console: http://localhost:9001"
echo "   🪣 MinIO API: http://localhost:9000"
echo ""
echo "🔑 MinIO Credentials:"
echo "   Username: minioadmin"
echo "   Password: minioadmin123"
echo ""
echo "📁 MinIO Bucket: gallery-images"
echo ""
echo "🛠️ To stop services: docker-compose down"
echo "📊 To view logs: docker-compose logs -f [service_name]"
echo "🔄 To restart a service: docker-compose restart [service_name]"
