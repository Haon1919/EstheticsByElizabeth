# Angular + C# Web API Project

This is a full-stack application with an Angular frontend and a C# Web API backend.

## Project Structure

```
src/
  ├── frontend/  # Angular application
  ├── backend/   # C# Web API
  └── scripts/   # Azure setup scripts
```

## Setup Instructions

### Prerequisites
- Node.js and npm for Angular
- .NET 8 SDK for C# API
- PowerShell for Azure scripts
- Azure CLI (for Azure deployments)

### Frontend Setup
1. Navigate to the frontend directory:
   ```
   cd src/frontend
   ```

2. Install dependencies:
   ```
   npm install
   ```

3. Start the development server:
   ```
   npm run start
   ```

### Backend Setup
1. Navigate to the backend directory:
   ```
   cd src/backend
   ```

2. Restore packages:
   ```
   dotnet restore
   ```

3. Run the API:
   ```
   dotnet run
   ```

### Azure SQL Database Setup
1. Make sure you have the Azure PowerShell module installed:
   ```
   Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -Force
   ```

2. Run the setup script with required parameters:
   ```
   pwsh src/scripts/setup-azure-sql.ps1 -ResourceGroupName "YourResourceGroup" -Location "EastUS" -ServerName "yourserver" -DatabaseName "yourdb" -AdminUsername "youradmin" -AdminPassword "yourpassword"
   ```

## Development Notes
- Frontend runs on `http://localhost:4200` by default
- Backend API runs on `http://localhost:5000` by default
- Configure CORS in the backend to allow frontend requests