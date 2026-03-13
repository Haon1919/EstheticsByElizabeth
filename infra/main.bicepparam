using 'main.bicep'

// Required — fill in before deploying
param dbAdminPassword = '' // Set via: --parameters dbAdminPassword='<value>'
param adminJwtSecret  = '' // Set via: --parameters adminJwtSecret='<value>'
param adminPassword   = '' // Set via: --parameters adminPassword='<value>'

// Customise these as needed
param location           = 'centralus'
param staticWebAppName   = 'esthetics-webapp'
param storageAccountName = 'estheticsstorage'  // must be globally unique, lowercase, 3-24 chars
param keyVaultName       = 'esthetics-kv'      // must be globally unique
param dbAdminLogin       = 'pgadmin'
param dbName             = 'estheticsdb'
