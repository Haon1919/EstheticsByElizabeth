#!/bin/bash

# Stop on any error
set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}==== PostgreSQL Connection Test ====${NC}"

# Connection parameters from your configuration
HOST="localhost"
PORT="5432"
DB="postgres"
USER="postgres"
PASSWORD="Ieatbugsandsquashfrogs"

# 1. Check if Docker container is running
echo -e "${BLUE}Checking if PostgreSQL container is running...${NC}"
if docker ps | grep -q local-postgres; then
    echo -e "${GREEN}✅ PostgreSQL container is running${NC}"
else
    echo -e "${RED}❌ PostgreSQL container is not running${NC}"
    echo -e "${BLUE}Starting the container...${NC}"
    cd /home/noah/test/docker && docker-compose up -d
    sleep 5 # Give it some time to start
    if docker ps | grep -q local-postgres; then
        echo -e "${GREEN}✅ PostgreSQL container started successfully${NC}"
    else
        echo -e "${RED}❌ Failed to start PostgreSQL container${NC}"
        exit 1
    fi
fi

# 2. Test connection using psql (PostgreSQL client)
echo -e "${BLUE}Testing connection with psql...${NC}"
if ! command -v psql &> /dev/null; then
    echo -e "${RED}❌ psql not found. Please install postgresql-client package${NC}"
    exit 1
fi

if PGPASSWORD="$PASSWORD" psql -h "$HOST" -p "$PORT" -d "$DB" -U "$USER" -c "SELECT 1" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Successfully connected to PostgreSQL using psql${NC}"
else
    echo -e "${RED}❌ Failed to connect to PostgreSQL using psql${NC}"
    exit 1
fi

# 3. Check if tables exist
echo -e "${BLUE}Checking if tables exist...${NC}"
TABLES=$(PGPASSWORD="$PASSWORD" psql -h "$HOST" -p "$PORT" -d "$DB" -U "$USER" -t -c "SELECT table_name FROM information_schema.tables WHERE table_schema='public';")

echo -e "${BLUE}Tables in database:${NC}"
echo "$TABLES"

# Check for specific tables from your schema
for TABLE in "clients" "categories" "services" "appointments" "clientreviewflags"; do
    if echo "$TABLES" | grep -q "$TABLE"; then
        echo -e "${GREEN}✅ Table '$TABLE' exists${NC}"
    else
        echo -e "${RED}❌ Table '$TABLE' does not exist${NC}"
    fi
done

echo -e "${GREEN}==== Connection test completed successfully ====${NC}"
