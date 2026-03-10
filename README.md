# Esthetics by Elizabeth

This is a full-stack application with an Angular frontend and a C# Azure Functions backend, using PostgreSQL for the database.

## Project Structure

```
.
├── frontend/  # Angular 19 application
├── api/       # C# .NET 8 Azure Functions backend
├── docker/    # Docker Compose setup for local development
└── tests/     # Backend xUnit tests
```

## Setup Instructions

### Prerequisites
- Node.js and npm (for Angular)
- .NET 8 SDK (for C# API)
- Azure Functions Core Tools (`func` CLI)
- Docker Desktop (for local database and storage emulation)

### Local Development Environment (Docker)
The easiest way to run the backend dependencies locally is using Docker Compose.

1. Navigate to the `docker` directory:
   ```bash
   cd docker
   ```

2. Copy the example environment file and configure it if necessary:
   ```bash
   cp .env.example .env
   ```

3. Start the services (PostgreSQL, MinIO, Azurite, MailHog):
   ```bash
   docker-compose up -d
   ```

### Backend Setup
1. Navigate to the backend directory:
   ```bash
   cd api
   ```

2. Restore packages:
   ```bash
   dotnet restore
   ```

3. Run the Azure Functions API:
   ```bash
   func start
   ```
   The API will run on `http://localhost:7071` by default.

### Frontend Setup
1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run start
   ```
   The frontend will run on `http://localhost:4200` by default.

## Development Notes
- **Database**: The project uses PostgreSQL. Entity Framework Core is used for data access. To manage database schema changes, use EF Core Migrations (e.g., `dotnet ef migrations add InitialCreate` and `dotnet ef database update`).
- **Storage**: Local development uses MinIO (S3-compatible) for image storage and Azurite for Azure Storage emulation.
- **Email**: Local development uses MailHog to capture outgoing emails. Access the MailHog UI at `http://localhost:8025`.