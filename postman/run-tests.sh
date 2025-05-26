#!/bin/bash

# Run Postman tests using Newman
# Script will automatically install Newman and the HTML reporter if needed

# Change to the postman directory
cd "$(dirname "$0")"

# Check if Newman is installed, install if not found
if ! command -v newman &> /dev/null; then
    echo "📦 Newman not found. Installing Newman and the HTML reporter..."
    npm install -g newman newman-reporter-htmlextra
    
    # Verify installation was successful
    if ! command -v newman &> /dev/null; then
        echo "❌ Failed to install Newman. Please install it manually: npm install -g newman newman-reporter-htmlextra"
        exit 1
    else
        echo "✅ Newman installed successfully!"
    fi
else
    echo "✅ Newman is already installed"
    
    # Check if HTML reporter is installed using npm list instead of running newman
    if ! npm list -g newman-reporter-htmlextra | grep -q "newman-reporter-htmlextra"; then
        echo "📦 Newman HTML reporter not found. Installing..."
        npm install -g newman-reporter-htmlextra
        echo "✅ Newman HTML reporter installed!"
    else
        echo "✅ Newman HTML reporter is already installed"
    fi
fi

# Ensure the API is running locally before testing
echo "Checking if the API is running on port 7071..."
API_RUNNING=false

# Try multiple endpoints to check if the API is running
if curl -s http://localhost:7071/api/health > /dev/null; then
  API_RUNNING=true
elif curl -s http://localhost:7071/api > /dev/null; then
  API_RUNNING=true
elif curl -s --max-time 2 http://localhost:7071 > /dev/null; then
  API_RUNNING=true
fi

if [ "$API_RUNNING" = false ]; then
  echo "⚠️ Warning: API doesn't seem to be running on port 7071."
  read -p "Do you want to continue anyway? (y/n) " -n 1 -r
  echo
  if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Exiting tests. Please start the API before running tests."
    exit 1
  fi
  echo "Continuing tests without API verification..."
else
  echo "✅ API is running on port 7071"
fi

# Run tests with Newman
echo "Running API tests with Newman..."
newman run appointment-api-collection.json \
  --environment local-environment.json \
  --reporters cli,htmlextra \
  --reporter-htmlextra-export ./newman-report.html \
  --reporter-htmlextra-title "Appointment API Test Results"

# Store the exit code
NEWMAN_EXIT_CODE=$?

# Check exit code
if [ $NEWMAN_EXIT_CODE -eq 0 ]; then
  echo "✅ All tests passed!"
  echo "📊 HTML report generated at: $(pwd)/newman-report.html"
  
  # Try to open the report in a browser if running in a desktop environment
  if [ -n "$DISPLAY" ]; then
    if command -v xdg-open &> /dev/null; then
      echo "Opening report in browser..."
      xdg-open ./newman-report.html
    fi
  fi
else
  echo "❌ Some tests failed with exit code: $NEWMAN_EXIT_CODE"
  echo "📊 HTML report generated at: $(pwd)/newman-report.html"
  
  # Try to open the report in a browser if running in a desktop environment
  if [ -n "$DISPLAY" ]; then
    if command -v xdg-open &> /dev/null; then
      echo "Opening report in browser..."
      xdg-open ./newman-report.html
    fi
  fi
  
  # Return the original exit code
  exit $NEWMAN_EXIT_CODE
fi
