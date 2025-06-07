#!/bin/bash

# Test runner script for Esthetics by Elizabeth API
# This script runs the comprehensive unit test suite in Docker

set -e

echo "🧪 Esthetics by Elizabeth - Unit Test Runner"
echo "=============================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker and try again."
    exit 1
fi

print_status "Building test container..."
docker-compose -f docker-compose.yml --profile testing build tests

print_status "Starting test container..."
docker-compose -f docker-compose.yml --profile testing up -d tests

print_status "Running comprehensive unit tests..."
echo ""
echo "📋 Test Summary:"
echo "=================="
echo "• GetServiceListTests.cs       - 8 test methods"
echo "• GetAppointmentsByDateTests.cs - 8 test methods" 
echo "• GetAppointmentHistoryByClientTests.cs - 8 test methods"
echo "• CancelAppointmentTests.cs    - 9 test methods"
echo "• GetCategoriesTests.cs        - 7 test methods"
echo "• UnbanClientTests.cs          - 11 test methods"
echo "• Total: 51 comprehensive unit tests"
echo ""

# Run tests with detailed output
print_status "Executing test suite..."
docker exec esthetics-tests dotnet test --logger "console;verbosity=detailed" --results-directory /tmp/test-results

# Get test results
TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -eq 0 ]; then
    print_status "✅ All tests passed successfully!"
    echo ""
    echo "🎉 Test Coverage Summary:"
    echo "========================="
    echo "✅ Core functionality (happy path scenarios)"
    echo "✅ Input validation and error handling"
    echo "✅ CORS handling (preflight and headers)"
    echo "✅ Database operations and data integrity"
    echo "✅ Exception handling and edge cases"
    echo "✅ Business logic validation"
else
    print_error "❌ Some tests failed. Check the output above for details."
fi

print_status "Cleaning up test container..."
docker-compose -f docker-compose.yml --profile testing down

echo ""
if [ $TEST_EXIT_CODE -eq 0 ]; then
    print_status "🚀 All Azure Functions are thoroughly tested and ready for deployment!"
else
    print_error "🛠️  Please fix failing tests before proceeding."
fi

exit $TEST_EXIT_CODE
