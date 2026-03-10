# Developer Guide

Welcome to the Esthetics by Elizabeth project! This guide will help you set up your local development environment and understand the codebase.

## 1. Tech Stack

*   **Frontend**: Angular 19 (TypeScript, HTML, CSS)
*   **Backend**: C# .NET 8 (Azure Functions Isolated Worker Model)
*   **Database**: PostgreSQL
*   **ORM**: Entity Framework Core
*   **Local Emulation**: Docker Compose (PostgreSQL, MinIO for S3/Blob storage, Azurite for Azure Storage, MailHog for email)

## 2. Prerequisites

Before you begin, ensure you have the following installed:
*   [Node.js](https://nodejs.org/) (v18+ recommended)
*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop)
*   [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local) (`npm i -g azure-functions-core-tools@4 --unsafe-perm true`)
*   [Angular CLI](https://angular.io/cli) (`npm install -g @angular/cli`)

## 3. Local Environment Setup

We use Docker Compose to spin up all external dependencies locally.

1.  **Start Docker Services**:
    ```bash
    cd docker
    cp .env.example .env  # Edit .env if you want to change default passwords
    docker-compose up -d
    ```
    This starts:
    *   PostgreSQL on `localhost:5432`
    *   MinIO (Image Storage) on `localhost:9000` (UI on `9001`)
    *   MailHog (Email testing) on `localhost:1025` (UI on `8025`)
    *   Azurite (Azure Storage Emulator) on `localhost:10000`

2.  **Run Database Migrations**:
    Ensure the database schema is up to date.
    ```bash
    cd api
    dotnet tool install --global dotnet-ef # If not already installed
    dotnet ef database update
    ```

3.  **Start the Backend API**:
    ```bash
    cd api
    func start
    ```
    The API will be available at `http://localhost:7071`.

4.  **Start the Frontend**:
    Open a new terminal window.
    ```bash
    cd frontend
    npm install
    npm start
    ```
    The Angular app will be available at `http://localhost:4200`.

## 4. Project Structure

```text
.
├── api/                    # C# Azure Functions Backend
│   ├── Data/               # EF Core DbContext and Migrations
│   ├── Entities/           # Database Models
│   ├── Functions/          # Azure Function Endpoints (Controllers)
│   ├── Services/           # Business Logic (Email, Storage, Auth)
│   └── local.settings.json # Local environment variables (gitignored)
├── frontend/               # Angular Application
│   ├── src/app/            # Components, Services, Models
│   └── public/images/      # Static assets
├── docker/                 # Local development infrastructure
└── tests/                  # xUnit test projects
```

## 5. Database Migrations

When you change a model in `api/Entities/`, you must create a migration:

```bash
cd api
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## 6. Testing

### Backend Tests
The backend uses xUnit for testing.
```bash
cd tests/API.Tests
dotnet test
```

### Email Testing
When running locally, the API is configured to send emails to MailHog instead of a real SMTP server.
To view emails sent by the application (e.g., appointment confirmations), open your browser to `http://localhost:8025`.

## 7. Code Style & Conventions

*   **C#**: Follow standard Microsoft C# conventions. Use dependency injection for services. Keep Functions (Controllers) thin and move business logic to the `Services/` folder.
*   **Angular**: Use standalone components. Keep components focused on presentation and use Angular Services for state management and API calls.
