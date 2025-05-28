# PostgreSQL Docker Setup

This directory contains everything needed to run a PostgreSQL database with all required tables for the appointment scheduling system.

## Files

- `docker-compose.yml`: Docker Compose configuration to run PostgreSQL
- `init-scripts/init-database.sql`: Combined SQL initialization script that runs automatically when the container is first created

## How It Works

The Docker Compose setup mounts the `init-scripts` directory to `/docker-entrypoint-initdb.d/` inside the container. PostgreSQL automatically runs any `.sql` files in this directory in alphabetical order when the container is first created.

The `init-database.sql` file:
1. Creates all required tables (Clients, Categories, Services, Appointments, ClientReviewFlags)
2. Sets up indexes for optimal performance
3. Seeds the database with initial data for categories and services
4. Resets sequence counters to account for explicitly specified IDs

## Usage

### Starting the Database

```bash
# From the docker directory
docker-compose up -d
```

### Viewing Logs

```bash
# Check initialization logs
docker logs local-postgres
```

### Connecting to the Database

```
Host: localhost
Port: 5432
Database: postgres
Username: postgres
Password: Ieatbugsandsquashfrogs
```

### Resetting the Database

If you need to completely reset the database (recreate all tables):

```bash
# Stop and remove the container
docker-compose down

# Remove the volume (THIS DELETES ALL DATA)
docker volume rm docker_postgres-data

# Start fresh
docker-compose up -d
```

### Notes

- The initialization scripts only run when the container is first created
- If you modify the SQL scripts, you'll need to recreate the container and volume to see changes
- The connection string in your application should match the settings here