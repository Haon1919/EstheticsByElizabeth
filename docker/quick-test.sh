#!/bin/bash

# Quick test runner - runs tests in existing container or creates one if needed
# Usage: ./quick-test.sh [test-filter]
# Example: ./quick-test.sh GetServiceListTests

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

# Check if test container exists and is running
if ! docker ps | grep -q "esthetics-tests"; then
    print_status "Test container not running. Starting it..."
    docker-compose -f docker/docker-compose.yml --profile testing up -d tests
    sleep 3
fi

# Run tests with optional filter
if [ $# -eq 0 ]; then
    print_status "Running all tests..."
    docker exec esthetics-tests dotnet test --logger "console;verbosity=normal"
else
    print_status "Running tests matching filter: $1"
    docker exec esthetics-tests dotnet test --filter "FullyQualifiedName~$1" --logger "console;verbosity=normal"
fi
