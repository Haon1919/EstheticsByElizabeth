#!/bin/bash

# Esthetics by Elizabeth - Database Migration Script
# This script runs Entity Framework migrations on your Azure database

set -e  # Exit on any error

# Load configuration
if [ ! -f ".azure-config" ]; then
    echo "❌ No .azure-config found. Please run ./scripts/deploy.sh first"
    exit 1
fi

source .azure-config
echo "📋 Loaded configuration from .azure-config"

# Attempt to load secrets (optional)
if [ -f .azure-secrets ]; then
  # shellcheck disable=SC1091
  source .azure-secrets
fi

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8 SDK"
    exit 1
fi

# Ensure Azure CLI login
if ! az account show &> /dev/null; then
  echo "Azure login required. Running az login..."
  az login >/dev/null
fi

# Detect if DB server is stopped
STATUS=$(az postgres flexible-server show --name "$DB_SERVER" --resource-group "$RESOURCE_GROUP" --query state -o tsv 2>/dev/null || echo "unknown")
if [ "$STATUS" = "Stopped" ]; then
  echo "⏸️ Database server is currently stopped."
  read -p "Start database server now? (y/N): " startdb
  if [[ "$startdb" =~ ^[Yy]$ ]]; then
    echo "▶️ Starting server $DB_SERVER ..."
    az postgres flexible-server start --name "$DB_SERVER" --resource-group "$RESOURCE_GROUP" --output table
    echo "Waiting 15s for server to become available..."
    sleep 15
  else
    echo "❌ Cannot run migrations while server is stopped. Exiting."
    exit 1
  fi
fi

# Navigate to API project
cd src/backend/API

echo "🗄️ Running database migrations on Azure PostgreSQL..."
echo "Database server: $DB_SERVER"

# Acquire DB password
if [ -z "${DB_PASSWORD:-}" ]; then
  echo "🔐 DB password not in memory; attempting to reconstruct connection string via Azure CLI."
  echo "If the password was rotated and not stored locally, you must supply it."
  read -s -p "Enter current DB admin password (dbadmin): " DB_PASSWORD
  echo ""
fi

# Get connection string
DB_CONNECTION=$(az postgres flexible-server show-connection-string \
    --server-name $DB_SERVER \
    --admin-user dbadmin \
    --admin-password "$DB_PASSWORD" \
    --database-name esthetics \
    --query connectionString \
    --output tsv)

# Set environment variable for migration
export ConnectionStrings__DefaultConnection="$DB_CONNECTION"

echo "🔄 Updating database schema..."

# Ensure dotnet-ef is available
if ! dotnet tool list -g | grep -q 'dotnet-ef'; then
  echo "Installing dotnet-ef tool globally..."
  dotnet tool install --global dotnet-ef
  export PATH="$HOME/.dotnet/tools:$PATH"
fi

# Run Entity Framework migrations
dotnet ef database update --verbose

echo ""
echo "✅ Database migration complete!"
echo "================================================"
echo "Your Azure database is now ready with the latest schema"
echo "================================================"

# Return to root directory
cd ../../..
