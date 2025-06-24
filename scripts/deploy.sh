#!/bin/bash

# Esthetics by Elizabeth - Azure Deployment Script
# This script creates all Azure resources needed for the application
# 
# Usage:
#   ./scripts/deploy.sh prepare   - Creates resource group and saves config (no charges)
#   ./scripts/deploy.sh activate  - Creates and starts all resources
#   ./scripts/deploy.sh full      - Creates and starts all resources (default)

set -e  # Exit on any error

# Get deployment mode
DEPLOY_MODE=${1:-"full"}

# Configuration Variables
RESOURCE_GROUP="esthetics-rg"
LOCATION="eastus"

# Check if we have existing config
if [ -f ".azure-config" ] && [ "$DEPLOY_MODE" = "activate" ]; then
    echo "📋 Loading existing configuration..."
    source .azure-config
else
    # Generate new resource names with timestamps
    DB_SERVER="esthetics-db-server-$(date +%s)"
    DB_PASSWORD="EstheticsDB2025!"
    STORAGE_ACCOUNT="estheticsstorage$(date +%s | cut -c6-)"
    WEBAPP_NAME="esthetics-webapp-$(date +%s)"
    GITHUB_REPO="https://github.com/yourusername/EstheticsByElizabeth"  # Update this with your actual repo
fi

echo "🚀 Starting Azure deployment for Esthetics by Elizabeth..."
echo "Mode: $DEPLOY_MODE"
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"

# Login check
echo "Checking Azure login..."
if ! az account show &> /dev/null; then
    echo "Please login to Azure first:"
    az login
fi

# Create resource group
echo "📦 Creating resource group..."
az group create \
    --name $RESOURCE_GROUP \
    --location $LOCATION \
    --output table

# Set default resource group and location
az configure --defaults group=$RESOURCE_GROUP location=$LOCATION

# Handle different deployment modes
if [ "$DEPLOY_MODE" = "prepare" ]; then
    echo ""
    echo "✅ PREPARE MODE Complete!"
    echo "================================================"
    echo "📋 Resource group created: $RESOURCE_GROUP"
    echo "🛑 No resources are running (no charges)"
    echo ""
    echo "Resource names reserved:"
    echo "🗄️ Database server: $DB_SERVER"
    echo "💾 Storage account: $STORAGE_ACCOUNT"
    echo "📱 Static Web App: $WEBAPP_NAME"
    echo ""
    echo "To create and start resources:"
    echo "  ./scripts/deploy.sh activate"
    echo "================================================"
    
    # Save configuration for later activation
    cat > .azure-config << EOF
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
DB_SERVER=$DB_SERVER
DB_PASSWORD=$DB_PASSWORD
STORAGE_ACCOUNT=$STORAGE_ACCOUNT
WEBAPP_NAME=$WEBAPP_NAME
GITHUB_REPO=$GITHUB_REPO
EOF
    
    echo "Configuration saved to .azure-config"
    exit 0
fi

# For 'activate' and 'full' modes, create all resources
echo "🚀 Creating all Azure resources..."

# Create PostgreSQL server (free tier)
echo "🗄️ Creating PostgreSQL database server..."
az postgres flexible-server create \
    --name $DB_SERVER \
    --admin-user dbadmin \
    --admin-password $DB_PASSWORD \
    --sku-name Standard_B1ms \
    --tier Burstable \
    --version 14 \
    --storage-size 32 \
    --public-access 0.0.0.0 \
    --output table

# Create database
echo "📊 Creating esthetics database..."
az postgres flexible-server db create \
    --server-name $DB_SERVER \
    --database-name esthetics \
    --output table

# Create storage account for blob storage
echo "💾 Creating storage account..."
az storage account create \
    --name $STORAGE_ACCOUNT \
    --sku Standard_LRS \
    --kind StorageV2 \
    --access-tier Hot \
    --output table

# Create blob container for images
echo "🖼️ Creating blob container for images..."
az storage container create \
    --name images \
    --account-name $STORAGE_ACCOUNT \
    --public-access blob \
    --output table

# Create Static Web App with Functions integration
echo "🌐 Creating Static Web App..."
az staticwebapp create \
    --name $WEBAPP_NAME \
    --source $GITHUB_REPO \
    --branch main \
    --app-location "/" \
    --api-location "src/backend/API" \
    --output-location "src/frontend/dist" \
    --output table

# Get connection strings
echo "🔗 Configuring connection strings..."
DB_CONNECTION=$(az postgres flexible-server show-connection-string \
    --server-name $DB_SERVER \
    --admin-user dbadmin \
    --admin-password $DB_PASSWORD \
    --database-name esthetics \
    --query connectionString \
    --output tsv)

STORAGE_CONNECTION=$(az storage account show-connection-string \
    --name $STORAGE_ACCOUNT \
    --query connectionString \
    --output tsv)

# Set application settings
echo "⚙️ Setting application configuration..."
az staticwebapp appsettings set \
    --name $WEBAPP_NAME \
    --setting-names \
        "ConnectionStrings__DefaultConnection=$DB_CONNECTION" \
        "AzureWebJobsStorage=$STORAGE_CONNECTION" \
        "Values__ImageStorage__Provider=Azure" \
        "Values__ImageStorage__ConnectionString=$STORAGE_CONNECTION" \
        "Values__ImageStorage__ContainerName=images" \
        "Values__Email__Provider=azure" \
    --output table

# Get the deployed URL
WEBAPP_URL=$(az staticwebapp show \
    --name $WEBAPP_NAME \
    --query "defaultHostname" \
    --output tsv)

echo ""
echo "✅ Deployment Complete!"
echo "================================================"
echo "🌐 Your app is available at: https://$WEBAPP_URL"
echo "🗄️ Database server: $DB_SERVER"
echo "💾 Storage account: $STORAGE_ACCOUNT"
echo "📱 Static Web App: $WEBAPP_NAME"
echo ""
echo "Next steps:"
echo "1. Push your code to GitHub to trigger automatic deployment"
echo "2. Add your custom domain (optional):"
echo "   az staticwebapp hostname set --name $WEBAPP_NAME --hostname yourdomain.com"
echo "3. Run database migrations if needed"
echo ""
echo "💰 Current monthly cost: ~$0 (within free tiers)"
echo "================================================"

# Save configuration to file for later use
cat > .azure-config << EOF
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
DB_SERVER=$DB_SERVER
DB_PASSWORD=$DB_PASSWORD
STORAGE_ACCOUNT=$STORAGE_ACCOUNT
WEBAPP_NAME=$WEBAPP_NAME
WEBAPP_URL=$WEBAPP_URL
GITHUB_REPO=$GITHUB_REPO
EOF

echo "Configuration saved to .azure-config"
