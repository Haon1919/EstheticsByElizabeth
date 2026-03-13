# Azure Deployment Scripts

> **📌 Prefer Infrastructure as Code?** See [`infra/README.md`](../infra/README.md) for the declarative **Azure Bicep** templates that provision the same resources in a repeatable, version-controlled way. The Bicep templates are the recommended approach for new deployments.

This folder contains **imperative** scripts to deploy and manage your Esthetics by Elizabeth application on Azure.

## Scripts Overview

### 🚀 `deploy.sh`
**Main deployment script** - Creates all Azure resources needed for the application with multiple modes:

**Usage modes:**
```bash
./scripts/deploy.sh prepare   # Creates resource group only (no charges)
./scripts/deploy.sh activate  # Creates all resources using saved config
./scripts/deploy.sh full      # Creates everything immediately (default)
```

**Prepare Mode:** (Recommended for testing)
- Creates resource group only
- Reserves resource names
- Saves configuration to `.azure-config`
- **Cost:** $0/month (no resources running)

**Activate Mode:**
- Uses saved configuration from prepare mode
- Creates all resources and starts them
- **Cost:** Uses free tier quotas

**Full Mode:** (Original behavior)
- Creates resource group and all resources immediately
- **Cost:** Uses free tier quotas immediately

### 🗑️ `cleanup.sh`
**Resource cleanup script** - Removes all Azure resources to avoid charges during development.

**Usage:**
```bash
./scripts/cleanup.sh
```

**What it does:**
- Deletes entire resource group
- Stops all charges
- Removes local configuration

### 🌐 `setup-domain.sh`
**Custom domain configuration** - Helps set up your custom domain (e.g., estheticsbyelizabeth.com).

**Usage:**
```bash
./scripts/setup-domain.sh
```

**What it does:**
- Configures custom domain on Static Web App
- Provides DNS configuration instructions
- Generates validation tokens

### 🗄️ `migrate-db.sh`
**Database migration script** - Runs Entity Framework migrations on your Azure database.

**Usage:**
```bash
./scripts/migrate-db.sh
```

**Prerequisites:**
- .NET SDK installed
- Run after `deploy.sh`

## Quick Start

1. **Prepare Infrastructure (Recommended for testing):**
   ```bash
   chmod +x scripts/*.sh
   ./scripts/deploy.sh prepare
   ```
   This creates the resource group and reserves names but doesn't start any billable resources.

2. **Activate When Ready:**
   ```bash
   ./scripts/deploy.sh activate
   ```
   This creates and starts all resources using the saved configuration.

3. **OR Full Deployment (Original approach):**
   ```bash
   ./scripts/deploy.sh full
   ```
   Creates everything immediately.

4. **Update GitHub Repository:**
   - Update the `GITHUB_REPO` variable in the saved `.azure-config` file
   - Push your code to trigger automatic deployment

5. **Database Setup:**
   ```bash
   ./scripts/migrate-db.sh
   ```

6. **Custom Domain (Optional):**
   ```bash
   ./scripts/setup-domain.sh
   ```

7. **Cleanup (Development):**
   ```bash
   ./scripts/cleanup.sh
   ```

## Configuration

The scripts automatically save configuration to `.azure-config` including:
- Resource group name
- Database server name
- Storage account name
- Web app name
- Application URL

## Prerequisites

- Azure CLI installed and configured
- GitHub repository for automatic deployment
- .NET SDK (for database migrations)

## Cost Management

- **Free Tier Resources:** All resources stay within Azure free tiers
- **Only Cost:** Custom domain registration (~$12/year)
- **Development:** Use `cleanup.sh` to avoid charges when not testing

## Troubleshooting

- **Login Issues:** Run `az login` if authentication fails
- **Resource Conflicts:** Resource names include timestamps to avoid conflicts
- **DNS Propagation:** Custom domains can take 24-48 hours to propagate

## Architecture

```
Custom Domain (optional)
    ↓
Azure Static Web Apps (free tier)
    ├── Frontend: Angular/HTML files
    └── API: Azure Functions (free tier)
         ↓
Azure Database for PostgreSQL (free tier)
Azure Blob Storage (free tier)
```

**Total Monthly Cost:** ~$0 (within free tiers) + domain registration
