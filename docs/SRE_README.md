# Site Reliability Engineering (SRE) Guide

This document outlines the operational procedures, monitoring, and incident response strategies for the Esthetics by Elizabeth application running in Azure.

## 1. Infrastructure Overview

The application is deployed entirely on Azure using a serverless and PaaS architecture to minimize operational overhead while maintaining high availability.

*   **Frontend & API Gateway**: Azure Static Web Apps (SWA)
*   **Compute (Backend)**: Azure Functions (.NET 8 Isolated Worker Model), managed by SWA.
*   **Database**: Azure Database for PostgreSQL (Flexible Server).
*   **Blob Storage**: Azure Storage Account (Standard_LRS) for gallery images.
*   **Email Delivery**: Azure Communication Services.

## 2. Deployment & CI/CD

Deployments are fully automated via GitHub Actions (`.github/workflows/azure-static-web-app.yml`).

### Deployment Flow
1.  Code is pushed to the `master` branch.
2.  GitHub Actions builds the Angular frontend and .NET backend.
3.  The artifacts are pushed to Azure Static Web Apps using the `Azure/static-web-apps-deploy@v1` action.
4.  Entity Framework Core migrations are automatically applied to the PostgreSQL database.

### Rollbacks
If a bad deployment occurs:
1.  Revert the commit in GitHub and push to `master`. The CI/CD pipeline will automatically deploy the previous stable state.
2.  **Database Rollbacks**: EF Core migrations are applied automatically, but rolling back a database schema requires manual intervention. Use `dotnet ef database update <PreviousMigrationName>` locally against the production database, or restore from a point-in-time backup.

## 3. Secrets Management

Secrets are **never** stored in source control.

*   **GitHub Actions**: Requires `AZURE_STATIC_WEB_APPS_API_TOKEN` and `DB_CONNECTION_STRING` stored as GitHub Repository Secrets.
*   **Azure Environment**: Application settings (environment variables) are managed within the Azure Static Web App configuration. These include:
    *   `ConnectionStrings__DefaultConnection`
    *   `Values__Auth__AdminJwtSecret`
    *   `Values__Admin__Password`
    *   `Values__ImageStorage__ConnectionString`

*To update a secret in production, use the Azure CLI:*
```bash
az staticwebapp appsettings set --name <WebAppName> --setting-names "Values__Admin__Password=NewPassword123!"
```

## 4. Monitoring & Observability

### Application Insights (Recommended Setup)
Currently, the project relies on basic Azure metrics. For professional SRE, **Azure Application Insights** must be enabled.
1.  Provision an Application Insights workspace.
2.  Add the `APPLICATIONINSIGHTS_CONNECTION_STRING` to the Static Web App settings.
3.  This will provide distributed tracing, exception logging, and performance metrics for the Azure Functions backend.

### Database Monitoring
Monitor the Azure PostgreSQL Flexible Server via the Azure Portal:
*   **CPU/Memory Usage**: Set alerts if CPU exceeds 80% for > 5 minutes.
*   **Active Connections**: Ensure connection pooling is effective.
*   **Storage Space**: Set alerts when storage reaches 85% capacity.

## 5. Disaster Recovery & Backups

### Database Backups
Azure PostgreSQL Flexible Server automatically handles backups.
*   **Retention**: Configured by default to 7 days (can be increased up to 35 days).
*   **Restore**: Point-in-Time Restore (PITR) is available via the Azure Portal or CLI to recover from accidental data deletion or corruption.

### Storage Account Backups
The Azure Storage Account holding gallery images is configured as `Standard_LRS` (Locally Redundant Storage).
*   **Action Item**: For production, consider enabling **Soft Delete** for blobs (retains deleted blobs for X days) to protect against accidental deletion by the admin panel.

## 6. Incident Response Playbook

### Scenario: API is returning 500 Internal Server Error
1.  Check GitHub Actions to see if a recent deployment failed or introduced a bug.
2.  Check Azure PostgreSQL metrics. Is the database reachable? Are connections maxed out?
3.  Check Azure Static Web Apps logs (or Application Insights if enabled) for stack traces.

### Scenario: Database is down or unresponsive
1.  Verify the server state via CLI: `az postgres flexible-server show --name <ServerName> --resource-group <RG> --query state`
2.  If stopped, start it: `az postgres flexible-server start ...`
3.  If running but unresponsive, check for compute/memory exhaustion and consider scaling up the SKU.

### Scenario: Images are not loading
1.  Verify the Azure Storage Account exists and the `images` container is set to `public-access blob`.
2.  Verify the `Values__ImageStorage__ConnectionString` in the SWA app settings is correct.

## 7. Infrastructure as Code (IaC)

The infrastructure is currently managed via imperative bash scripts (`scripts/deploy.sh`).
*   **Future Improvement**: Migrate to declarative IaC using **Terraform** or **Bicep** for better state management, drift detection, and reproducibility.
