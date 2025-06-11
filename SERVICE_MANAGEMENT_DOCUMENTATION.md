# Service Management System Documentation

## Overview

The Service Management System allows administrators to create, update, and delete services in the Esthetics by Elizabeth application. This feature provides a comprehensive admin interface for managing the service catalog.

## Features

### Backend API Endpoints

#### 1. Create Service
- **Endpoint**: `POST /admin/services`
- **Description**: Creates a new service
- **Request Body**:
  ```json
  {
    "name": "Service Name",
    "description": "Service description",
    "price": 99.99,
    "duration": 60,
    "categoryId": 1,
    "website": "https://example.com"
  }
  ```
- **Validation**:
  - Name is required (max 255 characters)
  - Price must be between 0 and 9999.99
  - Duration must be between 1 and 480 minutes
  - Category ID must exist
  - Website must be a valid URL
  - Service name must be unique within the category

#### 2. Update Service
- **Endpoint**: `PUT /admin/services/{serviceId}`
- **Description**: Updates an existing service
- **Request Body**: Same as create, but all fields are optional
- **Validation**: Same rules as create service

#### 3. Delete Service
- **Endpoint**: `DELETE /admin/services/{serviceId}`
- **Description**: Deletes a service
- **Safety Check**: Cannot delete services with existing appointments

### Frontend Admin Component

#### Route
- **Path**: `/admin/services`
- **Component**: `AdminServicesComponent`
- **Protected**: Requires admin authentication

#### Features

1. **Service Listing**
   - View all services grouped by category
   - Search functionality
   - Category filtering
   - Service count display

2. **Service Creation**
   - Form validation
   - Category selection dropdown
   - Price and duration input
   - Website URL field
   - Description text area

3. **Service Editing**
   - Pre-populated form with existing data
   - Same validation as creation
   - Updates service in real-time

4. **Service Deletion**
   - Confirmation dialog
   - Safety checks on backend
   - Real-time list updates

5. **User Interface**
   - Responsive design
   - Card-based service display
   - Professional styling
   - Loading states
   - Success/error messages

## Technical Implementation

### Backend Structure

```
Functions/
├── CreateService.cs      # POST /admin/services
├── UpdateService.cs      # PUT /admin/services/{id}
└── DeleteService.cs      # DELETE /admin/services/{id}
```

### Frontend Structure

```
components/admin-services/
├── admin-services.component.ts    # Main component logic
├── admin-services.component.html  # Template
└── admin-services.component.css   # Styles
```

### Data Models

#### Service Interface (Frontend)
```typescript
interface Service {
  id: number;
  name: string;
  description: string;
  price?: number;
  duration?: number;
  category: Category;
  website?: string;
}
```

#### Create/Update Request Models
```typescript
interface CreateServiceRequest {
  name: string;
  description?: string;
  price?: number;
  duration?: number;
  categoryId: number;
  website?: string;
}
```

## Validation Rules

### Service Name
- Required
- Maximum 255 characters
- Must be unique within category

### Description
- Optional
- Maximum 2000 characters

### Price
- Optional
- Must be between $0.00 and $9999.99
- Decimal precision to 2 places

### Duration
- Optional
- Must be between 1 and 480 minutes (8 hours)

### Category
- Required
- Must reference existing category

### Website
- Optional
- Must be valid URL (http:// or https://)
- Maximum 2048 characters

## User Workflow

### Adding a New Service

1. Admin logs into the system
2. Navigates to Service Management
3. Clicks "Add New Service"
4. Fills out the form:
   - Service name (required)
   - Category selection (required)
   - Description (optional)
   - Price (optional)
   - Duration (optional)
   - Website URL (optional)
5. Submits the form
6. Service is created and appears in the list

### Editing a Service

1. Admin finds the service in the list
2. Clicks the edit button (pencil icon)
3. Form pre-populates with current data
4. Admin makes changes
5. Submits the updated form
6. Service is updated in the list

### Deleting a Service

1. Admin finds the service in the list
2. Clicks the delete button (trash icon)
3. Confirms deletion in the dialog
4. Service is deleted if no appointments exist
5. Service is removed from the list

## Error Handling

### Backend Errors
- Validation errors return detailed messages
- Database errors are logged and return generic errors
- Constraint violations (e.g., service has appointments) return specific messages

### Frontend Errors
- Form validation prevents submission of invalid data
- API errors are displayed to the user
- Loading states prevent multiple submissions

## Security Considerations

- All admin endpoints are protected (though currently using Anonymous for development)
- Input validation on both frontend and backend
- SQL injection prevention through Entity Framework
- XSS prevention through Angular's built-in sanitization

## Future Enhancements

1. **Bulk Operations**
   - Import services from CSV
   - Bulk delete/update
   - Duplicate services

2. **Service Images**
   - Upload service photos
   - Image gallery

3. **Service Packages**
   - Bundle multiple services
   - Package pricing

4. **Service Scheduling**
   - Available time slots per service
   - Service-specific scheduling rules

5. **Analytics**
   - Popular services tracking
   - Revenue by service
   - Booking conversion rates

## Testing

### Manual Testing Checklist

- [ ] Create service with all fields
- [ ] Create service with only required fields
- [ ] Update service details
- [ ] Delete service without appointments
- [ ] Try to delete service with appointments
- [ ] Search services
- [ ] Filter by category
- [ ] Validate form errors
- [ ] Test responsive design

### API Testing

Use tools like Postman to test the backend endpoints:

1. **Create Service**
   ```bash
   POST http://localhost:7071/api/admin/services
   Content-Type: application/json

   {
     "name": "Test Service",
     "categoryId": 1,
     "price": 50.00
   }
   ```

2. **Update Service**
   ```bash
   PUT http://localhost:7071/api/admin/services/1
   Content-Type: application/json

   {
     "price": 60.00
   }
   ```

3. **Delete Service**
   ```bash
   DELETE http://localhost:7071/api/admin/services/1
   ```

## Navigation

The Service Management feature is accessible through:
- Admin menu: `/admin/services`
- Direct URL navigation for authenticated users
- Breadcrumb navigation in the admin interface

## Responsive Design

The interface adapts to different screen sizes:
- **Desktop**: Multi-column grid layout
- **Tablet**: Reduced columns, maintained functionality
- **Mobile**: Single column, stacked forms

This comprehensive service management system provides administrators with full control over the service catalog while maintaining data integrity and user experience.
