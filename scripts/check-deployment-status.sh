#!/bin/bash

# Azure Static Web App Status Checker
# This script helps diagnose deployment issues

echo "🔍 Azure Static Web App Deployment Diagnostics"
echo "=============================================="

# Get the app URL from the error message
APP_URL="https://salmon-mud-08e22b810.1.azurestaticapps.net"

echo "📱 App URL: $APP_URL"
echo ""

# Test the frontend
echo "🌐 Testing Frontend..."
curl -I "$APP_URL" 2>/dev/null | head -1

# Test the API health endpoint
echo ""
echo "🏥 Testing API Health Endpoint..."
curl -I "$APP_URL/api/health" 2>/dev/null | head -1

# Test the problematic endpoints
echo ""
echo "🏷️ Testing Categories Endpoint..."
curl -I "$APP_URL/api/categories" 2>/dev/null | head -1

echo ""
echo "💅 Testing Services Endpoint..."
curl -I "$APP_URL/api/services" 2>/dev/null | head -1

echo ""
echo "📊 Detailed API Response (Categories):"
curl -s "$APP_URL/api/categories" | head -c 200
echo ""

echo ""
echo "📊 Detailed API Response (Services):"
curl -s "$APP_URL/api/services" | head -c 200
echo ""

echo ""
echo "🔧 Troubleshooting Tips:"
echo "1. Check Azure Portal for Static Web App logs"
echo "2. Verify database connection string in App Settings"
echo "3. Check if Azure Functions are properly deployed"
echo "4. Ensure database is accessible from Azure"
echo "5. Check for any recent deployment failures"
