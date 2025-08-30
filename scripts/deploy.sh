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
    --fresh) FRESH=1 ;;
  esac
done

# Configuration Variables
RESOURCE_GROUP="esthetics-rg"
LOCATION="centralus"

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

# Load existing config also for full (idempotent) unless --fresh provided
if [ -f ".azure-config" ] && [[ "$DEPLOY_MODE" =~ ^(activate|full)$ ]] && [ -z "${FRESH:-}" ]; then
  echo "📋 Reusing existing configuration (.azure-config)"
  # shellcheck disable=SC1091
  source .azure-config
  REUSE=1
fi

# Generate new names only if not reusing
if [ -z "${REUSE:-}" ] && ! [[ "$DEPLOY_MODE" =~ ^(start-db|stop-db)$ ]]; then
  DB_SERVER="esthetics-db-server-$(date +%s)"
  STORAGE_ACCOUNT="estheticsstorage$(date +%s | cut -c6-)"
  WEBAPP_NAME="esthetics-webapp-$(date +%s)"
  ACS_NAME="esthetics-comm-$(date +%s)"
fi

# Auto-detect repo URL & branch if possible
if [ -z "${GITHUB_REPO:-}" ]; then
  GITHUB_REPO=$(git config --get remote.origin.url 2>/dev/null || true)
fi
if [ -z "${GITHUB_REPO:-}" ]; then
  GITHUB_REPO="https://github.com/Haon1919/EstheticsByElizabeth" # fallback
fi
GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo main)

# Load secrets early if present
if [ -f .azure-secrets ]; then
  # shellcheck disable=SC1091
  source .azure-secrets
fi

# Generate secrets only if not already specified (never leave blank). Only when creating new resources.
if [ -z "${REUSE:-}" ]; then
  if [ -z "${DB_PASSWORD:-}" ]; then DB_PASSWORD="$(rand_b64 25)"; fi
  if [ -z "${ADMIN_PASSWORD:-}" ]; then ADMIN_PASSWORD="$(rand_b64 20)"; fi
  if [ -z "${ADMIN_JWT_SECRET:-}" ]; then ADMIN_JWT_SECRET="$(rand_b64 48)"; fi
fi

# Optionally write secrets to a local (gitignored) file for operator reference
# Persist secrets file only if (a) new or (b) file missing
if [ ! -f .azure-secrets ] || [ -z "${REUSE:-}" ]; then
  SECRETS_FILE=".azure-secrets"
  echo "Writing secrets to $SECRETS_FILE (local only)..."
  cat > "$SECRETS_FILE" << EOF
# Local reference only. Do NOT commit.
DB_PASSWORD=$DB_PASSWORD
ADMIN_PASSWORD=$ADMIN_PASSWORD
ADMIN_JWT_SECRET=$ADMIN_JWT_SECRET
EOF
  chmod 600 "$SECRETS_FILE" 2>/dev/null || true
fi

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

# (Optional) Show active subscription
ACTIVE_SUB=$(az account show --query name -o tsv 2>/dev/null || true)
if [ -n "$ACTIVE_SUB" ]; then
  echo "Azure Subscription: $ACTIVE_SUB"
fi

# Create resource group (idempotent)
echo "📦 Ensuring resource group exists..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

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

# For 'activate' and 'full' modes, create/ensure all resources
# Helper: test existence
pg_exists() { az postgres flexible-server show --name "$DB_SERVER" --resource-group "$RESOURCE_GROUP" &>/dev/null; }
store_exists() { az storage account show --name "$STORAGE_ACCOUNT" --resource-group "$RESOURCE_GROUP" &>/dev/null; }
acs_exists() { az communication list --resource-group "$RESOURCE_GROUP" --query "[?name=='$ACS_NAME']|length(@)" -o tsv 2>/dev/null | grep -q '^1$'; }
swa_exists() { az staticwebapp show --name "$WEBAPP_NAME" &>/dev/null; }

echo "🚀 Ensuring Azure resources..."

# PostgreSQL server
if pg_exists; then
  echo "🗄️ PostgreSQL server exists: $DB_SERVER (skipping create)"
else
  echo "🗄️ Creating PostgreSQL database server $DB_SERVER..."
  az postgres flexible-server create \
    --name $DB_SERVER \
    --admin-user dbadmin \
    --admin-password $DB_PASSWORD \
    --sku-name Standard_B1ms \
    --tier Burstable \
    --version 14 \
    --storage-size 32 \
    --public-access 0.0.0.0 \
    --output none
  CREATED_DB=1
fi

if [ "${CREATED_DB:-}" = 1 ] && [ "${DB_STOP_AFTER_CREATE:-}" = "1" ]; then
  echo "⏸️ Stopping DB server immediately to avoid compute charges..."
  az postgres flexible-server stop --name "$DB_SERVER" --resource-group "$RESOURCE_GROUP" --output none
  echo "ℹ️ DB server stopped. Start later with: ./scripts/deploy.sh start-db"
fi

# Database
if az postgres flexible-server db show --server-name "$DB_SERVER" --database-name esthetics &>/dev/null; then
  echo "📊 Database esthetics exists (skipping create)"
else
  echo "📊 Creating esthetics database..."
  az postgres flexible-server db create --server-name $DB_SERVER --database-name esthetics --output none
fi

# Static Web App
if swa_exists; then
  echo "🌐 Static Web App exists: $WEBAPP_NAME (skipping create)"
else
  echo "🌐 Creating Static Web App $WEBAPP_NAME (branch: $GIT_BRANCH)..."
  az staticwebapp create \
    --name $WEBAPP_NAME \
    --source $GITHUB_REPO \
    --branch $GIT_BRANCH \
    --app-location "frontend" \
    --api-location "api" \
    --output-location "dist/app" \
    --token $GITHUB_TOKEN \
    --output none
fi

# Communication Services
if acs_exists; then
  echo "📧 Azure Communication Service exists: $ACS_NAME"
else
  echo "📧 Creating Azure Communication Services (Email) $ACS_NAME..."
  az extension add --name communication &>/dev/null || true
  az communication create --name $ACS_NAME --resource-group $RESOURCE_GROUP --data-location unitedstates --location global --output none
fi
ACS_CONNECTION=$(az communication list-key --name $ACS_NAME --resource-group $RESOURCE_GROUP --query primaryConnectionString -o tsv)

# Storage account
if store_exists; then
  echo "💾 Storage account exists: $STORAGE_ACCOUNT"
else
  echo "💾 Creating storage account $STORAGE_ACCOUNT..."
  az storage account create --name $STORAGE_ACCOUNT --sku Standard_LRS --kind StorageV2 --access-tier Hot --output none
fi
# Container
if az storage container show --name images --account-name $STORAGE_ACCOUNT &>/dev/null; then
  echo "🖼️ Blob container 'images' exists"
else
  echo "🖼️ Creating blob container 'images'..."
  az storage container create --name images --account-name $STORAGE_ACCOUNT --public-access blob --output none >/dev/null
fi

# Storage connection string
STORAGE_CONNECTION=$(az storage account show-connection-string --name $STORAGE_ACCOUNT --query connectionString -o tsv)

# Get connection strings
echo "🔗 Configuring connection strings..."
# Connection string (only if have DB_PASSWORD)
if [ -n "${DB_PASSWORD:-}" ]; then
  DB_CONNECTION=$(az postgres flexible-server show-connection-string \
    --server-name $DB_SERVER \
    --admin-user dbadmin \
    --admin-password $DB_PASSWORD \
    --database-name esthetics \
    --query connectionString -o tsv)
else
  echo "⚠️ DB password not available; skipping DB connection string update."
fi

# STORAGE_CONNECTION is already set above

# Set application settings
# Application settings (always ensure non-secret settings; include secrets if present)
APPSET_ARGS=("--name" "$WEBAPP_NAME" "--setting-names"
  "AzureWebJobsStorage=$STORAGE_CONNECTION"
  "Values__ImageStorage__Provider=Azure"
  "Values__ImageStorage__ConnectionString=$STORAGE_CONNECTION"
  "Values__ImageStorage__ContainerName=images"
  "Values__Email__Provider=azure"
  "Values__Email__Azure__ConnectionString=$ACS_CONNECTION"
  "Values__Email__Azure__FromEmail=noreply@estheticsbyelizabeth.com"
)
if [ -n "${DB_CONNECTION:-}" ]; then APPSET_ARGS+=("ConnectionStrings__DefaultConnection=$DB_CONNECTION"); fi
if [ -n "${ADMIN_PASSWORD:-}" ]; then APPSET_ARGS+=("Values__Admin__Password=$ADMIN_PASSWORD"); fi
if [ -n "${ADMIN_JWT_SECRET:-}" ]; then APPSET_ARGS+=("Values__Auth__AdminJwtSecret=$ADMIN_JWT_SECRET"); fi

echo "⚙️ Setting application configuration (idempotent)..."
az staticwebapp appsettings set "${APPSET_ARGS[@]}" --output none

# Get the deployed URL
WEBAPP_URL=$(az staticwebapp show --name $WEBAPP_NAME --query "defaultHostname" -o tsv)

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
# Save configuration (always up to date)
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
