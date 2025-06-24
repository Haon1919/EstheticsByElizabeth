#!/bin/bash

# Esthetics by Elizabeth - Custom Domain Setup Script
# This script helps configure a custom domain for your Static Web App

set -e  # Exit on any error

# Load configuration
if [ ! -f ".azure-config" ]; then
    echo "❌ No .azure-config found. Please run ./scripts/deploy.sh first"
    exit 1
fi

source .azure-config
echo "📋 Loaded configuration from .azure-config"

# Get domain name from user
echo "🌐 Custom Domain Setup for Esthetics by Elizabeth"
echo ""
read -p "Enter your custom domain (e.g., estheticsbyelizabeth.com): " CUSTOM_DOMAIN

if [ -z "$CUSTOM_DOMAIN" ]; then
    echo "❌ Domain name cannot be empty"
    exit 1
fi

echo ""
echo "Setting up custom domain: $CUSTOM_DOMAIN"
echo "Static Web App: $WEBAPP_NAME"

# Add custom domain to Static Web App
echo "🔗 Adding custom domain to Static Web App..."
az staticwebapp hostname set \
    --name $WEBAPP_NAME \
    --hostname $CUSTOM_DOMAIN \
    --output table

# Get validation token
echo ""
echo "📋 Getting domain validation information..."
VALIDATION_TOKEN=$(az staticwebapp hostname show \
    --name $WEBAPP_NAME \
    --hostname $CUSTOM_DOMAIN \
    --query "validationToken" \
    --output tsv)

STATIC_IP=$(az staticwebapp show \
    --name $WEBAPP_NAME \
    --query "customDomains[?hostname=='$CUSTOM_DOMAIN'].ipAddress" \
    --output tsv)

echo ""
echo "✅ Custom domain configuration started!"
echo "================================================"
echo "📝 DNS Configuration Required:"
echo ""
echo "Add these DNS records with your domain registrar:"
echo ""
echo "1. CNAME Record (preferred):"
echo "   Name: @ (or leave blank for root domain)"
echo "   Value: $WEBAPP_URL"
echo ""
echo "2. OR A Record (alternative):"
echo "   Name: @ (or leave blank for root domain)"  
echo "   Value: [IP address will be provided after CNAME validation]"
echo ""
echo "3. TXT Record (for validation):"
echo "   Name: asuid.$CUSTOM_DOMAIN"
echo "   Value: $VALIDATION_TOKEN"
echo ""
echo "⏱️ DNS propagation can take 24-48 hours"
echo "================================================"
echo ""
echo "To check validation status:"
echo "  az staticwebapp hostname show --name $WEBAPP_NAME --hostname $CUSTOM_DOMAIN"
echo ""
echo "Once validated, your site will be available at:"
echo "  https://$CUSTOM_DOMAIN"
