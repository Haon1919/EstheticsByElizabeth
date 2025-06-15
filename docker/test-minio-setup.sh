#!/bin/bash

# 🧪 Test MinIO Image Storage Setup 🧪
# This script tests the MinIO storage implementation

echo "🧪 Testing MinIO Image Storage Setup..."

# Change to the docker directory
cd "$(dirname "$0")"

# Start services if not running
echo "📦 Ensuring services are running..."
docker-compose up -d

# Wait for services to be ready
echo "⏳ Waiting for services to initialize..."
sleep 15

# Check if MinIO is accessible
echo "🔍 Testing MinIO connectivity..."
curl -f http://localhost:9000/minio/health/live || {
    echo "❌ MinIO health check failed"
    exit 1
}

# Check if API is accessible
echo "🔍 Testing API connectivity..."
curl -f http://localhost/api/manage/gallery/upload/info || {
    echo "❌ API health check failed"
    exit 1
}

# Test image upload (you can uncomment this to test with a real image)
# echo "📤 Testing image upload..."
# curl -X POST -F "image=@test-image.jpg" http://localhost/api/manage/gallery/upload

echo "✅ All tests passed!"
echo ""
echo "🎉 MinIO Image Storage is ready to use!"
echo ""
echo "📊 Service Status:"
docker-compose ps
