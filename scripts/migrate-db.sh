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

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8 SDK"
    exit 1
fi

# Navigate to API project
cd src/backend/API

echo "🗄️ Running database migrations on Azure PostgreSQL..."
echo "Database server: $DB_SERVER"

# Get connection string
DB_CONNECTION=$(az postgres flexible-server show-connection-string \
    --server-name $DB_SERVER \
    --admin-user dbadmin \
    --admin-password "EstheticsDB2025!" \
    --database-name esthetics \
    --query connectionString \
    --output tsv)

# Set environment variable for migration
export ConnectionStrings__DefaultConnection="$DB_CONNECTION"

echo "🔄 Updating database schema..."

# Run Entity Framework migrations
dotnet ef database update --verbose

echo ""
echo "✅ Database migration complete!"
echo "================================================"
echo "Your Azure database is now ready with the latest schema"
echo "================================================"

# Return to root directory
cd ../../..
