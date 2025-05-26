# Appointment API Postman Tests

This directory contains Postman collections and environments for testing the Appointment API, with a focus on the client review flag functionality.

## Files

- `appointment-api-collection.json`: The main Postman collection with all API tests
- `local-environment.json`: Environment variables for local testing
- `newman-config.json`: Configuration for running tests with Newman
- `run-tests.sh`: Bash script to run the tests using Newman

## Running Tests

### Using Postman GUI

1. Import the collection: In Postman, click "Import" and select `appointment-api-collection.json`
2. Import the environment: In Postman, click "Import" and select `local-environment.json`
3. Select the "Local Development" environment from the environment dropdown
4. Run the collection: Click on the collection and then click "Run"

### Using Newman (Command Line)

1. Install Newman and the HTML reporter:
   ```bash
   npm install -g newman newman-reporter-htmlextra
   ```

2. Run the tests using the provided script:
   ```bash
   ./run-tests.sh
   ```

   Or run Newman directly:
   ```bash
   newman run appointment-api-collection.json \
     --environment local-environment.json \
     --reporters cli,htmlextra \
     --reporter-htmlextra-export ./newman-report.html
   ```

3. Check the generated HTML report for test results.

## Test Workflow

The tests validate the entire appointment scheduling workflow:

1. Get available services
2. Schedule an appointment
3. Try to schedule an appointment for a client under review (should fail)
4. Get appointments for a specific date
5. Get appointment history for a client
6. Cancel an appointment
7. View and update client review flags

## Notes

- The tests assume a local API running on port 7071
- The client review tests validate the foreign key constraint between ClientReviewFlag and Appointment
- Test data is automatically generated during the test run
