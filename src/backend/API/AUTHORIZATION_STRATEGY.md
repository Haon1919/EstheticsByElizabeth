# API Authorization Strategy

This document outlines which endpoints require admin authorization and which remain public.

## Authorization Implementation

Admin endpoints use JWT token validation via `AuthTokenService.ValidateRequest(req)`. The token is issued by the `/auth/admin` endpoint after successful password authentication.

## Public Endpoints (No Authorization Required)

### Authentication
- `POST /auth/admin` - Admin login (must remain anonymous)

### Public Website Features
- `GET /services` - Service list for public services page and booking
- `GET /categories` - Category list for public services page and booking  
- `GET /gallery` - Public gallery images for gallery page
- `POST /contact` - Contact form submissions
- `POST /appointments` - Public appointment booking
- `GET /appointments/earliest-date` - Available booking dates

## Admin-Only Endpoints (Authorization Required)

### Service Management
- `POST /manage/services` - Create service
- `PUT /manage/services/{id}` - Update service  
- `DELETE /manage/services/{id}` - Delete service

### Category Management
- `POST /manage/categories` - Create category
- `PUT /manage/categories/{id}` - Update category
- `DELETE /manage/categories/{id}` - Delete category
- `GET /manage/categories/service-count` - Get service counts by category

### Appointment Management
- `GET /appointments/date/{date}` - Get appointments by date/range (admin calendar view)
- `GET /appointments/history` - Get client appointment history (admin client search)
- `DELETE /appointments/{id}` - Cancel appointment (admin management)

### Contact Management
- `GET /manage/contacts` - Get contact submissions
- `PUT /manage/contacts/{id}/status` - Update submission status
- `PUT /manage/contacts/{id}/notes` - Update submission notes
- `DELETE /manage/contacts/{id}` - Delete submission

### Gallery Management
- `GET /manage/gallery` - Get all gallery images (admin view)
- `POST /manage/gallery` - Create gallery image
- `PUT /manage/gallery/{id}` - Update gallery image
- `DELETE /manage/gallery/{id}` - Delete gallery image
- `PUT /manage/gallery/reorder` - Reorder gallery images
- `GET /manage/gallery/categories` - Get gallery categories with counts

### Client Review System
- `GET /client-reviews` - Get client review flags
- `GET /client-reviews/{id}` - Get specific review flag
- `PUT /client-reviews/{id}` - Update review flag status
- `POST /client-reviews` - Create review flag
- `DELETE /client-reviews/{id}` - Delete review flag
- `POST /clients/{id}/ban` - Ban client
- `DELETE /clients/{id}/ban` - Unban client
- `GET /clients/{id}/reviews` - Get client pending reviews

### Image Upload
- `POST /upload` - Upload images (admin gallery management)
- `GET /upload/presigned-url` - Get presigned upload URL

## Frontend Route Protection

The Angular frontend protects admin routes using `AuthGuard`:

### Public Routes (No Auth Guard)
- `/` - Home
- `/services` - Services page
- `/booking` - Booking page  
- `/gallery` - Gallery page
- `/contact` - Contact page
- `/aftercare` - Aftercare page
- `/admin` - Admin login page

### Protected Routes (Auth Guard)
- `/admin/submissions` - Contact submissions management
- `/admin/appointments` - Appointment management
- `/admin/clients` - Client management
- `/admin/services` - Service management
- `/admin/categories` - Category management
- `/admin/gallery` - Gallery management

## Security Notes

1. **JWT Token**: Expires after 12 hours, requires valid ADMIN_JWT_SECRET
2. **CORS**: All endpoints include appropriate CORS headers
3. **Public Safety**: Public booking endpoint includes client review system to prevent abuse
4. **Admin Access**: All admin functionality requires valid authentication token
5. **Separation**: Clear separation between public customer-facing features and admin management

## Token Flow

1. Admin enters password on `/admin` page
2. Frontend calls `POST /auth/admin` with password
3. Backend validates against `ADMIN_PASSWORD` environment variable
4. Backend returns JWT token if valid
5. Frontend stores token and includes in Authorization header for admin API calls
6. Admin endpoints validate token using `AuthTokenService.ValidateRequest()`
