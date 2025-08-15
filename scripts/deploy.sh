#!/bin/bash

# Esthetics by Elizabeth - Azure Deployment Script
# This script creates all Azure resources needed for the application
# 
# Usage:
#   ./scripts/deploy.sh prepare                 - Creates resource group only (no cost)
#   ./scripts/deploy.sh activate [--stop-db-after-create] - Creates resources; optionally stops DB after creation
#   ./scripts/deploy.sh full [--stop-db-after-create]     - Same as activate (default)
#   ./scripts/deploy.sh start-db               - Starts existing PostgreSQL server
#   ./scripts/deploy.sh stop-db                - Stops existing PostgreSQL server (halt compute charges)
#   Env alternative: set DB_STOP_AFTER_CREATE=1 to auto-stop DB after create

set -e  # Exit on any error

# Get deployment mode
DEPLOY_MODE=${1:-"full"}
shift || true
# Parse optional flags
for arg in "$@"; do
  case "$arg" in
    --stop-db-after-create) DB_STOP_AFTER_CREATE=1 ;;
  esac
done

# Configuration Variables
RESOURCE_GROUP="esthetics-rg"
LOCATION="eastus"

# Helper to generate random values (length param optional)
rand_b64() {
  local LEN=${1:-32}
  # Remove characters Azure may not like in some contexts and trim length
  openssl rand -base64 $((LEN*2)) | tr -d '=+/' | cut -c1-${LEN}
}

# If performing start/stop operations, load config early
if [[ "$DEPLOY_MODE" =~ ^(start-db|stop-db)$ ]]; then
  if [ ! -f .azure-config ]; then
    echo "Missing .azure-config. Run activate/full first." >&2
    exit 1
  fi
  # shellcheck disable=SC1091
  source .azure-config
  echo "Using resource group: $RESOURCE_GROUP"
  if [ -z "${DB_SERVER:-}" ]; then
    echo "DB_SERVER not found in .azure-config" >&2
    exit 1
  fi
  if [ "$DEPLOY_MODE" = "start-db" ]; then
    echo "▶️ Starting PostgreSQL server $DB_SERVER ..."
    az postgres flexible-server start --name "$DB_SERVER" --resource-group "$RESOURCE_GROUP" --output table
    echo "✅ Database server started."
    exit 0
  elif [ "$DEPLOY_MODE" = "stop-db" ]; then
    echo "⏸️ Stopping PostgreSQL server $DB_SERVER ..."
    az postgres flexible-server stop --name "$DB_SERVER" --resource-group "$RESOURCE_GROUP" --output table
    echo "✅ Database server stopped (compute billing paused; storage persists)."
    exit 0
  fi
fi

# Check if we have existing config
if [ -f ".azure-config" ] && [ "$DEPLOY_MODE" = "activate" ]; then
    echo "📋 Loading existing configuration..."
    source .azure-config
else
    # Generate new resource names with timestamps
    DB_SERVER="esthetics-db-server-$(date +%s)"
    STORAGE_ACCOUNT="estheticsstorage$(date +%s | cut -c6-)"
    WEBAPP_NAME="esthetics-webapp-$(date +%s)"
    ACS_NAME="esthetics-comm-$(date +%s)"
    GITHUB_REPO="https://github.com/yourusername/EstheticsByElizabeth"  # Update this with your actual repo
fi

# Generate secrets only if not already specified (never leave blank)
if [ -z "${DB_PASSWORD:-}" ]; then
  DB_PASSWORD="$(rand_b64 25)"
fi
if [ -z "${ADMIN_PASSWORD:-}" ]; then
  ADMIN_PASSWORD="$(rand_b64 20)"
fi
# JWT secret should be at least 32 chars
if [ -z "${ADMIN_JWT_SECRET:-}" ]; then
  ADMIN_JWT_SECRET="$(rand_b64 48)"
fi

# Optionally write secrets to a local (gitignored) file for operator reference
SECRETS_FILE=".azure-secrets"
echo "Writing generated secrets to $SECRETS_FILE (local only)..."
cat > "$SECRETS_FILE" << EOF
# Local reference only. Do NOT commit.
DB_PASSWORD=$DB_PASSWORD
ADMIN_PASSWORD=$ADMIN_PASSWORD
ADMIN_JWT_SECRET=$ADMIN_JWT_SECRET
EOF
chmod 600 "$SECRETS_FILE" 2>/dev/null || true

echo "🚀 Starting Azure deployment for Esthetics by Elizabeth..."
echo "Mode: $DEPLOY_MODE"
# Avoid echoing secrets
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
    echo "📧 Communication Services: $ACS_NAME"
    echo ""
    echo "To create and start resources:"
    echo "  ./scripts/deploy.sh activate"
    echo "================================================"
    
    # Save configuration for later activation
    cat > .azure-config << EOF
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
DB_SERVER=$DB_SERVER
# DB_PASSWORD intentionally omitted (stored only in .azure-secrets)
STORAGE_ACCOUNT=$STORAGE_ACCOUNT
WEBAPP_NAME=$WEBAPP_NAME
ACS_NAME=$ACS_NAME
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

# If requested, stop DB immediately after creation
if [ "${DB_STOP_AFTER_CREATE:-}" = "1" ]; then
  echo "⏸️ Stopping DB server immediately to avoid compute charges..."
  az postgres flexible-server stop --name "$DB_SERVER" --resource-group "$RESOURCE_GROUP" --output table
  echo "ℹ️ DB server is now stopped. Start later with: ./scripts/deploy.sh start-db"
fi

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

# Create Azure Communication Services for email
echo "📧 Creating Azure Communication Services (Email)..."
az extension add --name communication &>/dev/null || true
az communication create \
    --name $ACS_NAME \
    --resource-group $RESOURCE_GROUP \
    --data-location unitedstates \
    --output table
ACS_CONNECTION=$(az communication list-key \
    --name $ACS_NAME \
    --resource-group $RESOURCE_GROUP \
    --query primaryConnectionString \
    --output tsv)

# Get storage connection string (for app settings)
STORAGE_CONNECTION=$(az storage account show-connection-string \
    --name $STORAGE_ACCOUNT \
    --query connectionString \
    --output tsv)

# Create Static Web App with Functions integration
echo "🌐 Creating Static Web App..."
az staticwebapp create \
    --name $WEBAPP_NAME \
    --source $GITHUB_REPO \
    --branch main \
    --app-location "src/frontend" \
    --api-location "src/backend/API" \
    --output-location "dist/app" \
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

# STORAGE_CONNECTION is already set above

# Set application settings
echo "⚙️ Setting application configuration (including admin credentials & secrets)..."
az staticwebapp appsettings set \
    --name $WEBAPP_NAME \
    --setting-names \
        "ConnectionStrings__DefaultConnection=$DB_CONNECTION" \
        "AzureWebJobsStorage=$STORAGE_CONNECTION" \
        "Values__ImageStorage__Provider=Azure" \
        "Values__ImageStorage__ConnectionString=$STORAGE_CONNECTION" \
        "Values__ImageStorage__ContainerName=images" \
        "Values__Email__Provider=azure" \
        "Values__Email__Azure__ConnectionString=$ACS_CONNECTION" \
        "Values__Email__Azure__FromEmail=noreply@estheticsbyelizabeth.com" \
        "Values__Admin__Password=$ADMIN_PASSWORD" \
        "Values__Auth__AdminJwtSecret=$ADMIN_JWT_SECRET" \
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
echo "📧 Communication Services: $ACS_NAME"
echo ""
echo "Next steps:"
echo "1. Push your code to GitHub to trigger automatic deployment"
echo "2. Add your custom domain (optional):"
echo "   az staticwebapp hostname set --name $WEBAPP_NAME --hostname yourdomain.com"
echo "3. Run database migrations if needed"
echo ""
echo "💰 Current monthly cost: ~$0 (within free tiers)"
echo "================================================"

# Save configuration to file for later use (exclude sensitive secrets)
cat > .azure-config << EOF
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
DB_SERVER=$DB_SERVER
# DB_PASSWORD excluded
STORAGE_ACCOUNT=$STORAGE_ACCOUNT
WEBAPP_NAME=$WEBAPP_NAME
WEBAPP_URL=$WEBAPP_URL
ACS_NAME=$ACS_NAME
ACS_CONNECTION=$ACS_CONNECTION
GITHUB_REPO=$GITHUB_REPO
EOF

echo "Configuration saved to .azure-config"
