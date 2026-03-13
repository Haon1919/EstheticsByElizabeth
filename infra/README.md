# Azure Infrastructure as Code (Bicep)

This directory contains **Azure Bicep** templates that declaratively define all the Azure resources for Esthetics by Elizabeth.

## Resources Provisioned

| Resource | Purpose |
|----------|---------|
| **PostgreSQL Flexible Server** | Application database (Burstable B1ms, 32 GB) |
| **Storage Account** | Blob storage for images and Azure Functions runtime |
| **Static Web App** | Hosts the Angular frontend + linked Azure Functions API |
| **Key Vault** | Securely stores database password, JWT secret, and admin password |

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) v2.61+
- An Azure subscription
- Logged in: `az login`

## Deploying

1. **Create a resource group** (one-time):
   ```bash
   az group create --name esthetics-rg --location centralus
   ```

2. **Deploy the infrastructure**:
   ```bash
   az deployment group create \
     --resource-group esthetics-rg \
     --template-file infra/main.bicep \
     --parameters infra/main.bicepparam \
     --parameters dbAdminPassword='<STRONG_PASSWORD>' \
                  adminJwtSecret='<RANDOM_SECRET>' \
                  adminPassword='<ADMIN_PASSWORD>'
   ```

3. **View outputs** (database hostname, Static Web App URL, Key Vault URI):
   ```bash
   az deployment group show \
     --resource-group esthetics-rg \
     --name main \
     --query properties.outputs
   ```

## Tearing Down

```bash
az group delete --name esthetics-rg --yes --no-wait
```

## Migrating from Shell/PowerShell Scripts

This Bicep template replaces the imperative scripts in `scripts/`:
- `scripts/setup-azure-sql.ps1` → `postgresServer` + `postgresDb` resources
- `scripts/update-azure-resources.ps1` → re-run the Bicep deployment (idempotent)
- `scripts/deploy.sh` (resource creation portion) → this Bicep file

The deployment script (`scripts/deploy.sh`) can still be used for application-level deployment (code push) while Bicep handles infrastructure.
