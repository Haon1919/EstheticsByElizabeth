#!/bin/bash

# 🚀 Start EstheticsByElizabeth Services 🚀
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

# Check if image uploader completed successfully
echo "🎨 Checking image upload status..."
docker-compose logs image-uploader

# Wait a bit more for everything to settle
sleep 5

# Check service status
echo "🔍 Checking service status..."
docker-compose ps

# Show logs for troubleshooting
echo "📝 Recent API logs:"
docker-compose logs --tail=20 api

echo ""
echo "✅ Services are running!"
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
echo "🎨 Gallery images: automatically uploaded to MinIO"
echo ""
echo "🛠️ To stop services: docker-compose down"
echo "📊 To view logs: docker-compose logs -f [service_name]"
echo "🔄 To restart a service: docker-compose restart [service_name]"
echo "🧪 To test setup: ./test-minio-setup.sh"
