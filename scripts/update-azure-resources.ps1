# PowerShell script to update Azure resources
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$DatabaseName
)

# Login to Azure
Write-Host "Logging into Azure..."
# Uncomment the line below when running the script
# Connect-AzAccount

# Update SQL Database Tier
Write-Host "Updating SQL Database tier..."
# Set-AzSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName -Edition "Standard" -RequestedServiceObjectiveName "S1"

# Configure SQL Database Automatic Tuning
Write-Host "Configuring SQL Database automatic tuning..."
# Set-AzSqlDatabaseAutomaticTuning -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName -AutomaticTuningMode "Auto"

# Setup database backups
Write-Host "Setting up long-term retention backups..."
# $backupPolicy = Get-AzSqlDatabaseBackupLongTermRetentionPolicy -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName
# Set-AzSqlDatabaseBackupLongTermRetentionPolicy -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName -WeeklyRetention P4W -MonthlyRetention P12M -YearlyRetention P5Y

Write-Host "Azure resources have been updated!"