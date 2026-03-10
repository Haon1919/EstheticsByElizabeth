# Architecture Overview

This document provides a high-level overview of the system architecture, design decisions, and data flow for the Esthetics by Elizabeth application.

## 1. System Architecture

The application follows a modern, cloud-native, serverless architecture designed for low cost, high scalability, and minimal maintenance.

### High-Level Diagram

```text
[ Client Browser ]
       |
       | (HTTPS)
       v
[ Azure Static Web Apps ]
       |
       +---> [ Static Content (Angular) ]
       |
       +---> [ API Gateway / Routing ]
                   |
                   v
         [ Azure Functions (.NET 8) ]
                   |
       +-----------+-----------+
       |           |           |
       v           v           v
[ PostgreSQL ] [ Blob Storage ] [ Azure Comm. Services ]
  (Data)        (Images)        (Email)
```

## 2. Component Breakdown

### 2.1 Frontend (Angular 19)
*   **Role**: Single Page Application (SPA) providing the user interface for clients (booking, gallery) and the admin (managing appointments, services).
*   **Hosting**: Served globally via Azure Static Web Apps CDN.
*   **Design**: Uses standalone components for modularity. Communicates with the backend exclusively via REST APIs.

### 2.2 Backend (Azure Functions - .NET 8 Isolated)
*   **Role**: Stateless API layer handling business logic, authentication, and database interactions.
*   **Why Azure Functions?**: Provides a true serverless model. It scales to zero when not in use (saving money) and scales out automatically during traffic spikes. The Isolated Worker model ensures the .NET process runs independently of the Azure Functions host, allowing full control over dependencies and middleware.
*   **Key Patterns**:
    *   **Dependency Injection**: Used extensively for services (e.g., `IEmailService`, `IImageStorageService`).
    *   **Repository Pattern (via EF Core)**: `ProjectContext` acts as the Unit of Work and Repository.

### 2.3 Database (PostgreSQL)
*   **Role**: Primary relational data store for Clients, Appointments, Services, Categories, and Contact Submissions.
*   **Why PostgreSQL?**: Open-source, highly reliable, and supported natively by Azure Flexible Server. It provides better JSON support and spatial features than SQL Server if needed in the future.
*   **ORM**: Entity Framework Core is used for schema management (Code-First Migrations) and LINQ-to-SQL querying.

### 2.4 Storage (Azure Blob Storage)
*   **Role**: Stores unstructured data, specifically uploaded images for the Gallery.
*   **Abstraction**: The backend uses an `IImageStorageService` interface. In local development, this is implemented by `MinioImageStorageService` (S3-compatible). In production, it uses Azure Blob Storage. This abstraction prevents vendor lock-in.

### 2.5 Communication (Azure Communication Services)
*   **Role**: Handles transactional emails (e.g., "Your appointment is confirmed").
*   **Abstraction**: Similar to storage, an `IEmailService` interface is used. Local development uses `SmtpEmailService` (pointing to MailHog), while production uses `AzureEmailService`.

## 3. Security & Authentication

*   **Admin Panel**: Secured using JWT (JSON Web Tokens). The `AdminLogin` function validates credentials against a hashed password (or environment variable) and issues a token. Subsequent admin requests must include this token in the `Authorization: Bearer <token>` header.
*   **CORS**: Azure Static Web Apps automatically handles CORS between the frontend and the managed Azure Functions backend.
*   **Database Security**: The database is not exposed to the public internet. It is only accessible by the Azure Functions backend via connection strings stored securely in Azure App Settings.

## 4. Scalability & Future Considerations

*   **Current Bottleneck**: The PostgreSQL database is the only non-serverless component (running on a Burstable B1ms tier). If traffic increases significantly, this will be the first component that needs vertical scaling.
*   **Caching**: Currently, there is no distributed cache (like Redis). If read-heavy operations (like fetching the gallery or service list) become slow, implementing caching at the API layer or using Azure API Management would be the next architectural step.
*   **Background Jobs**: Currently, tasks like sending reminder emails might be handled by Timer-triggered Azure Functions. As the system grows, moving to a message queue (Azure Service Bus) for asynchronous processing of emails and notifications would improve API response times and reliability.
