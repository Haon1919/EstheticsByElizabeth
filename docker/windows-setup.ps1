<#
.SYNOPSIS
    Automates the installation of Git and Docker, and clones a specified Git repository.

.DESCRIPTION
    This script performs the following actions:
    1. Checks for Administrator privileges, as they are required for installation.
    2. Checks for and installs the Chocolatey package manager if it's not found.
    3. Uses Chocolatey to install Git and Docker Desktop. Docker Compose is included with Docker Desktop.
    4. Clones a user-defined Git repository to a local directory.

.PARAMETER GitRepoUrl
    The HTTPS URL of the Git repository to clone.

.PARAMETER TargetDir
    The local directory where the Git repository will be cloned.

.EXAMPLE
    .\Setup-DevEnvironment.ps1 -GitRepoUrl "https://github.com/your-username/your-repo.git" -TargetDir "C:\Projects\MyProject"

    This command will install Git and Docker, then clone 'your-repo' into 'C:\Projects\MyProject'.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string]$GitRepoUrl,

    [Parameter(Mandatory=$true)]
    [string]$TargetDir
)

#==============================================================================
# 1. PRE-FLIGHT CHECKS
#==============================================================================

# Check for Administrator Privileges
Write-Host "Checking for Administrator privileges..." -ForegroundColor Yellow
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Administrator privileges are required to install software. Please re-run this script from an elevated PowerShell terminal."
    # The 'break' command will stop the script in its tracks.
    break
}
Write-Host "Success: Running with Administrator privileges." -ForegroundColor Green

#==============================================================================
# 2. INSTALL DEPENDENCIES
#==============================================================================

# Function to check if a command/executable is available
function Test-CommandExists {
    param ($command)
    return (Get-Command $command -ErrorAction SilentlyContinue)
}

# --- Install Chocolatey Package Manager ---
Write-Host "Checking for Chocolatey package manager..." -ForegroundColor Yellow
if (-NOT (Test-CommandExists 'choco')) {
    Write-Host "Chocolatey not found. Installing..."
    try {
        Set-ExecutionPolicy Bypass -Scope Process -Force
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072;
        iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
        Write-Host "Chocolatey installed successfully." -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install Chocolatey. Please install it manually and re-run the script."
        break
    }
}
else {
    Write-Host "Chocolatey is already installed." -ForegroundColor Green
}

# --- Install Git and Docker ---
$packages = @('git', 'docker-desktop')

foreach ($pkg in $packages) {
    Write-Host "Checking for $pkg..." -ForegroundColor Yellow
    # 'choco list --local-only' checks for locally installed packages
    $isInstalled = choco list --local-only --exact $pkg | Select-String $pkg
    if ($isInstalled) {
        Write-Host "$pkg is already installed." -ForegroundColor Green
    }
    else {
        Write-Host "Installing $pkg via Chocolatey... This may take some time." -ForegroundColor Yellow
        try {
            choco install $pkg --yes --force
            Write-Host "$pkg installed successfully." -ForegroundColor Green
        }
        catch {
            Write-Error "Failed to install $pkg using Chocolatey. Please try installing it manually."
            # Continue to the next package even if one fails
        }
    }
}

#==============================================================================
# 3. CLONE REPOSITORY
#==============================================================================

# Check if Docker Desktop is running. It must be started to execute docker commands.
# This check remains useful as a user might want to run docker commands manually after the script.
Write-Host "Checking Docker Desktop status..." -ForegroundColor Yellow
$dockerProcess = Get-Process -Name "Docker Desktop" -ErrorAction SilentlyContinue
if (-not $dockerProcess) {
    Write-Host "Docker Desktop is not running. Attempting to start it..."
    try {
        Start-Process -FilePath "C:\Program Files\Docker\Docker\Docker Desktop.exe"
        Write-Host "Docker Desktop is starting. Please wait for it to be fully operational before running any Docker commands." -ForegroundColor Cyan
        # Give Docker some time to initialize.
        Start-Sleep -Seconds 45
    }
    catch {
         Write-Warning "Could not start Docker Desktop automatically. Please start it manually before running docker commands."
    }
} else {
    Write-Host "Docker Desktop is already running." -ForegroundColor Green
}

# --- Clone the Git Repository ---
Write-Host "Cloning repository from '$GitRepoUrl'..." -ForegroundColor Yellow
if (Test-Path -Path $TargetDir) {
    Write-Warning "Target directory '$TargetDir' already exists. Skipping clone."
}
else {
    try {
        git clone $GitRepoUrl $TargetDir
        Write-Host "Repository cloned successfully to '$TargetDir'." -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to clone the repository. Please check the URL and your network connection."
        break
    }
}

Write-Host "--- Environment setup complete. ---" -ForegroundColor Magenta
Write-Host "Next steps: Navigate to '$TargetDir' and run your project-specific commands."