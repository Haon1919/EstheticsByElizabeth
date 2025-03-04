# PowerShell script to provision Azure SQL Database
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location,
    
    [Parameter(Mandatory=$true)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$DatabaseName,
    
    [Parameter(Mandatory=$true)]
    [string]$AdminUsername,
    
    [Parameter(Mandatory=$true)]
    [string]$AdminPassword
)

# Login to Azure
Write-Host "Logging into Azure..."
# Uncomment the line below when running the script
# Connect-AzAccount

# Create a resource group
Write-Host "Creating resource group $ResourceGroupName in $Location..."
# New-AzResourceGroup -Name $ResourceGroupName -Location $Location

# Create an Azure SQL Server
Write-Host "Creating SQL Server $ServerName..."
# $securePassword = ConvertTo-SecureString $AdminPassword -AsPlainText -Force
# $serverCreds = New-Object System.Management.Automation.PSCredential ($AdminUsername, $securePassword)
# New-AzSqlServer -ResourceGroupName $ResourceGroupName -Location $Location -ServerName $ServerName -SqlAdministratorCredentials $serverCreds

# Configure a firewall rule to allow Azure services
Write-Host "Configuring firewall rule to allow Azure services..."
# New-AzSqlServerFirewallRule -ResourceGroupName $ResourceGroupName -ServerName $ServerName -AllowAllAzureIPs

# Create a SQL Database
Write-Host "Creating SQL Database $DatabaseName..."
# New-AzSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName -Edition "Standard" -RequestedServiceObjectiveName "S0"

Write-Host "SQL Database setup completed!"
Write-Host "Connection string: Server=tcp:$ServerName.database.windows.net,1433;Initial Catalog=$DatabaseName;Persist Security Info=False;User ID=$AdminUsername;Password=$AdminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"