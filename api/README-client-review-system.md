# Client Appointment Review System

This document explains how the client appointment review system works.

## Overview

The system helps prevent abuse of the appointment booking system by implementing a review mechanism for clients who attempt to book multiple appointments on the same day. The system:

1. Checks if a client already has an appointment on the requested day
2. Allows the appointment but flags the client for admin review
3. Provides an interface for administrators to review and make decisions on flagged bookings
4. Prevents clients with accounts under review from making additional appointments

## Client States

A client can be in one of the following states:

1. **Normal** - No review flags, or all review flags have been approved
2. **Under Review** - Has at least one booking flagged for review with "Pending" status
3. **Rejected** - An admin has rejected one of their bookings, preventing future bookings
4. **Banned** - An admin has explicitly banned the client from making bookings

## Flow Diagram

```
Client books first appointment
↓
ALLOWED (no flags)
↓
Client books second appointment on same day
↓
ALLOWED but FLAGGED (Pending status)
↓
Admin reviews flag:
   |
   ├── Approves → Client can continue booking (Normal state)
   |
   ├── Rejects → Client can't make new bookings (Rejected state)
   |
   └── Bans → Client can't make new bookings (Banned state)
```

## Implementation Details

The system uses the following components:

- `ClientReviewFlag` entity to track review flags
- `ScheduleAppointment` function to check for multiple bookings and create flags
- `CheckClientReviewStatusAsync` method to check client status
- `FlagClientForReviewAsync` method to create review flags
- `ManageClientReviews` function to handle admin reviews

## Database Schema

The `clientreviewflags` table has the following structure:

- `Id` - Primary key
- `ClientId` - Foreign key to the client
- `AppointmentId` - Foreign key to the triggering appointment
- `FlagReason` - Reason for the flag
- `FlagDate` - When the flag was created
- `ReviewedBy` - Who reviewed the flag
- `ReviewDate` - When the flag was reviewed
- `Status` - "Pending", "Approved", "Rejected", or "Banned"
- `AdminComments` - Comments from the admin
- `AutoFlags` - Number of times auto-flagged

## Importance of the AppointmentId Field

Including the `AppointmentId` in the `ClientReviewFlag` entity provides several important benefits:

1. **Traceability**: Directly links the review flag to the specific appointment that triggered it
2. **Context for Admin Review**: Administrators can see exactly which appointment caused the flag
3. **Action Targeting**: Admins can take specific actions on the appointment that triggered the flag
4. **Audit Trail**: Creates a complete history connecting flags to specific appointments

## Using the Review System

Administrators can use the following endpoints to manage the review system:

- `GET /api/client-reviews` - View all flagged clients
- `GET /api/client-reviews/{id}` - View a specific flag
- `PUT /api/client-reviews/{id}` - Update a flag's status
- `GET /api/clients/{clientId}/reviews` - View all reviews for a specific client