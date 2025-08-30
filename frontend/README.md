# App

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.0.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.

# Future Plans
# Instagram Integration & Scheduling System Architecture

## Overview

This architecture combines GitHub Pages for frontend hosting with Azure services for backend functionality. The system has two main components:
1. Instagram feed integration with hourly updates
2. Scheduling system with customer communications

## Core Components

### GitHub Pages
- Static website hosting (HTML, CSS, JavaScript)
- Custom domain with SSL
- Free hosting with version control integration

### Azure Functions
1. **Instagram Feed Function**
   - Timer-triggered to run hourly
   - Scrapes Instagram for latest posts
   - Updates feed JSON in Blob Storage
   
2. **Scheduling HTTP Function**
   - Receives booking requests from website
   - Stores booking data in Cosmos DB
   - Sends email notifications to business owner
   - Validates schedule availability

3. **Data Migration Function**
   - Monitors Cosmos DB size
   - Migrates older data to Blob Storage when approaching free tier limits
   - Maintains data archive for analytics and compliance

### Azure Storage Solutions
1. **Cosmos DB**
   - Stores active customer data and bookings
   - Maintains consent records
   - Enables fast queries for communications
   - Free tier: 400 RU/s and 5GB storage

2. **Blob Storage**
   - Stores Instagram feed JSON for website consumption
   - Archives historical booking data
   - Provides cost-effective long-term storage
   - Used for data analytics

### Azure Communication Services
- SMS notifications for appointment reminders
- Optional voice capabilities
- Tracking of all customer communications
- Pay-as-you-go pricing model

## Data Flow

1. **Instagram Feed Flow**
   ```
   Hourly Timer → Azure Function → Instagram Scraping → Blob Storage → GitHub Pages Website
   ```

2. **Booking Flow**
   ```
   Customer → GitHub Pages Form → HTTP Function → Cosmos DB → Email Notification → Business Owner
   ```

3. **Automated Communications Flow**
   ```
   Timer/Event → Azure Function → Cosmos DB Query → Azure Communication Services → Customer
   ```

4. **Data Migration Flow**
   ```
   Storage Monitor → Migration Function → Query Old Records → Write to Blob → Delete from Cosmos DB
   ```

## Cost Optimization Strategy

- Utilize free tiers across all services
- Monitor Cosmos DB usage (5GB free limit)
- Automatically tier data storage (hot data in Cosmos DB, cold data in Blob)
- Stay within Azure Functions free tier (1 million executions)
- Minimize Communication Services costs with targeted messaging

## Compliance & Security

- Store customer consent records with timestamps
- Maintain communication history
- Secure API endpoints with authentication
- HTTPS for all communications
- Data backup and disaster recovery through Azure

## Future Expansion Possibilities

- Analytics dashboard for business insights
- Customer portal for self-service booking management
- Expanded communication capabilities (email marketing, promotional campaigns)
- Integration with other social media platforms beyond Instagram

This architecture provides a robust, scalable solution while minimizing costs by leveraging free tiers and optimizing resource usage.