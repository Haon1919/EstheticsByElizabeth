# Docker Setup for Esthetics by Elizabeth API

This directory contains Docker configuration files to run the entire application stack locally using Docker containers.

## Services

- **PostgreSQL Database**: Running on port 5432
- **API Backend**: .NET 8 Azure Functions app running on port 80
- **Test Runner**: .NET 8 SDK container for running unit tests (testing profile)

## Files

- `docker-compose.yml`: Main compose file with service definitions
- `docker-compose.dev.yml`: Development overrides
- `Dockerfile.tests`: Test container configuration
- `start-services.sh`: Helper script to start all services
- `stop-services.sh`: Helper script to stop all services
- `run-tests.sh`: Comprehensive unit test runner
- `quick-test.sh`: Quick test runner for development
- `init-scripts/init-database.sql`: Database initialization script

## How It Works

The setup includes both the PostgreSQL database and the .NET API backend:

1. **Database**: PostgreSQL container with automatic initialization
2. **API**: .NET 8 Azure Functions container that depends on the database
3. **Health Checks**: Ensures the database is ready before starting the API
4. **Networking**: Services can communicate using container names

The database initialization:
1. Creates all required tables (Clients, Categories, Services, Appointments, ClientReviewFlags)
2. Sets up indexes for optimal performance
3. Seeds the database with initial data for categories and services
4. Resets sequence counters to account for explicitly specified IDs

## Quick Start

### Prerequisites

- Docker and Docker Compose installed on your machine
- At least 4GB of available RAM for Docker

### Starting the Services

1. Navigate to the docker directory:
   ```bash
   cd docker
   ```

2. Start all services using the helper script:
   ```bash
   ./start-services.sh
   ```

   Or manually with docker-compose:
   ```bash
   docker-compose up --build -d
   ```

3. The services will be available at:
   - **API**: http://localhost:7071
   - **Database**: localhost:5432
     - Username: `postgres`
     - Password: `Ieatbugsandsquashfrogs`
     - Database: `postgres`

### Stopping the Services

```bash
./stop-services.sh
```

Or manually:
```bash
docker-compose down
```

## Development

### Development Mode

For development with additional debugging features:

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d
```

## Running Tests

The project includes comprehensive unit tests for all Azure Functions. There are two ways to run tests:

### Comprehensive Test Suite

Run the full test suite with detailed reporting:

```bash
./run-tests.sh
```

This script will:
- Build a dedicated test container
- Run all 51 unit tests across 6 Azure Functions
- Provide detailed test results and coverage summary
- Clean up containers when finished

### Quick Testing (Development)

For faster testing during development:

```bash
./quick-test.sh
```

Or run specific tests:
```bash
./quick-test.sh GetServiceListTests
./quick-test.sh UnbanClient
```

### Test Coverage

The unit tests cover:
- ✅ **GetServiceListTests** (8 tests) - Service retrieval, CORS, ordering, error handling
- ✅ **GetAppointmentsByDateTests** (8 tests) - Date filtering, validation, CORS
- ✅ **GetAppointmentHistoryByClientTests** (8 tests) - Client history, email validation
- ✅ **CancelAppointmentTests** (9 tests) - Appointment cancellation, validation, errors
- ✅ **GetCategoriesTests** (7 tests) - Category retrieval and management
- ✅ **UnbanClientTests** (11 tests) - Client unbanning, status management, validation

**Total: 51 comprehensive unit tests** covering all core functionality, validation, CORS handling, error scenarios, and business logic.

### Viewing Logs

View logs for all services:
```bash
docker-compose logs -f
```

View logs for a specific service:
```bash
docker-compose logs -f api
docker-compose logs -f postgres
```

### Rebuilding Services

If you make changes to the API code, rebuild the API service:
```bash
docker-compose up --build api -d
```

## Database

### Connecting to Database

You can connect to the PostgreSQL database using any PostgreSQL client:

- **Host**: localhost
- **Port**: 5432
- **Database**: postgres
- **Username**: postgres
- **Password**: Ieatbugsandsquashfrogs

### Resetting Database

To completely reset the database and start fresh:

```bash
docker-compose down -v
docker-compose up --build -d
```

## Troubleshooting

### Service Startup Issues

1. Check if ports 5432 and 7071 are available:
   ```bash
   lsof -i :5432
   lsof -i :7071
   ```

2. View service logs:
   ```bash
   docker-compose logs api
   docker-compose logs postgres
   ```

### Database Connection Issues

1. Ensure PostgreSQL is healthy:
   ```bash
   docker-compose ps postgres
   ```

2. Test database connectivity:
   ```bash
   docker-compose exec postgres pg_isready -U postgres
   ```

### API Issues

1. Check if the API container is running:
   ```bash
   docker-compose ps api
   ```

2. Check API logs for startup errors:
   ```bash
   docker-compose logs --tail=50 api
   ```

# Remove the volume (THIS DELETES ALL DATA)
docker volume rm docker_postgres-data

# Start fresh
docker-compose up -d
```

### Notes

- The initialization scripts only run when the container is first created
- If you modify the SQL scripts, you'll need to recreate the container and volume to see changes
- The connection string in your application should match the settings here