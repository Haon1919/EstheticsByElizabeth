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
echo "🔍 Extended Diagnostics..."

# Check if the functions are actually deployed
echo "📦 Checking Functions Deployment:"
curl -s "$APP_URL/api/" 2>/dev/null | head -c 200 || echo "No response"

# Check Azure Static Web Apps configuration
echo ""
echo "⚙️ Checking SWA Configuration:"
curl -s "$APP_URL/.well-known/staticwebapps.config" 2>/dev/null | head -c 200 || echo "No config found"

# Test with different headers
echo ""
echo "🌐 Testing with User-Agent:"
curl -s -H "User-Agent: Mozilla/5.0" "$APP_URL/api/health" | head -c 200 || echo "No response"

# Check verbose curl output for health endpoint
echo ""
echo "🔍 Verbose Health Check:"
curl -s "$APP_URL/api/health" -v 2>&1 | grep -E "(HTTP|error|timeout|refused)" || echo "No verbose info"

echo ""
echo "🔧 Next Steps:"
echo "1. Check Azure Portal → Static Web App → Functions → Logs"
echo "2. Verify Application Settings in Azure Portal"
echo "3. Check if database is accessible from Azure"
echo "4. Review the latest deployment logs in GitHub Actions"
echo "5. If 503 persists, Functions runtime is not starting properly"
