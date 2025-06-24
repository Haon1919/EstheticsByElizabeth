#!/bin/bash

# Esthetics by Elizabeth - Azure Cleanup Script
# This script removes all Azure resources to avoid charges during development

set -e  # Exit on any error

# Load configuration if it exists
if [ -f ".azure-config" ]; then
    source .azure-config
    echo "📋 Loaded configuration from .azure-config"
    echo "Resource Group: $RESOURCE_GROUP"
else
    echo "⚠️ No .azure-config found. Using default values."
    RESOURCE_GROUP="esthetics-rg"
fi

echo "🗑️ Starting Azure resource cleanup..."
echo "This will DELETE all resources in: $RESOURCE_GROUP"
echo ""

# Confirmation prompt
read -p "Are you sure you want to delete all resources? (yes/no): " confirmation
if [ "$confirmation" != "yes" ]; then
    echo "❌ Cleanup cancelled"
    exit 0
fi

# Login check
echo "Checking Azure login..."
if ! az account show &> /dev/null; then
    echo "Please login to Azure first:"
    az login
fi

# Check if resource group exists
if ! az group exists --name $RESOURCE_GROUP; then
    echo "✅ Resource group $RESOURCE_GROUP doesn't exist. Nothing to clean up."
    exit 0
fi

# List resources before deletion
echo "📊 Current resources in $RESOURCE_GROUP:"
az resource list --resource-group $RESOURCE_GROUP --output table

echo ""
echo "🗑️ Deleting resource group and all resources..."
echo "This may take a few minutes..."

# Delete the entire resource group (this removes all resources)
az group delete \
    --name $RESOURCE_GROUP \
    --yes \
    --no-wait

echo ""
echo "✅ Cleanup initiated!"
echo "================================================"
echo "Resource group '$RESOURCE_GROUP' deletion started"
echo "This will continue in the background."
echo ""
echo "To check deletion status:"
echo "  az group show --name $RESOURCE_GROUP"
echo ""
echo "💰 All charges stopped once resources are deleted"
echo "================================================"

# Remove local configuration file
if [ -f ".azure-config" ]; then
    rm .azure-config
    echo "🗑️ Removed .azure-config file"
fi

echo ""
echo "To redeploy later, run: ./scripts/deploy.sh"
