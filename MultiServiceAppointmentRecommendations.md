# Multi-Service Appointment Support Analysis

## Current Data Model Limitations

After examining the current data model, I've identified the following limitations for supporting multi-service appointments:

1. **One-to-One Relationship**: Currently, an `Appointment` is directly linked to a single `Service` through the `ServiceId` foreign key.
   ```csharp
   public int ServiceId { get; set; }
   public virtual Service Service { get; set; }
   ```

2. **No Grouping Mechanism**: There's no way to group related appointments together that might be part of the same booking session.

3. **No Time Sequence**: If a client books multiple services, there's no way to specify the order or sequence of those services.

4. **Pricing and Duration**: The current model doesn't support calculating combined pricing or duration for multiple services.

## Recommendations for Multi-Service Support

### Option 1: Appointment Groups with Junction Table

Create a new data model that introduces an `AppointmentGroup` and a many-to-many relationship between `Appointment` and `Service`:

```csharp
public class AppointmentGroup
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public DateTimeOffset BookingDate { get; set; }
    public string Status { get; set; } = "Confirmed";
    public decimal TotalPrice { get; set; }
    public int TotalDuration { get; set; } // in minutes

    public virtual Client Client { get; set; } = null!;
    public virtual ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
}

public class AppointmentService
{
    public int Id { get; set; }
    public int AppointmentGroupId { get; set; }
    public int ServiceId { get; set; }
    public int SequenceOrder { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public decimal Price { get; set; } // Captured at booking time
    
    public virtual AppointmentGroup AppointmentGroup { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
}
```

### Option 2: Extend Current Model with AppointmentItems

Keep the current `Appointment` entity but add an `AppointmentItem` entity to represent multiple services:

```csharp
// Modify existing Appointment class
public class Appointment
{
    // Existing properties...
    
    // Remove direct ServiceId reference
    // public int ServiceId { get; set; }
    
    // Add new properties
    public decimal TotalPrice { get; set; }
    public int TotalDuration { get; set; } // in minutes
    
    // Navigation properties
    public virtual Client Client { get; set; } = null!;
    public virtual ICollection<AppointmentItem> Items { get; set; } = new List<AppointmentItem>();
}

// New entity
public class AppointmentItem
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int ServiceId { get; set; }
    public int SequenceOrder { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public decimal Price { get; set; } // Price at time of booking
    
    public virtual Appointment Appointment { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
}
```

### Option 3: Migration Path (Recommended)

To minimize disruption to the existing system, implement a gradual migration:

1. **Phase 1**: Add an `AppointmentGroup` table but maintain backward compatibility:
   - Add `AppointmentGroupId` (nullable) to the existing `Appointment` table
   - Single-service appointments continue to work as before
   - Multi-service appointments use the new grouping field

2. **Phase 2**: Migrate the API to use the new model:
   - Update the `ScheduleAppointment` function to handle multiple services
   - Create a new endpoint for multi-service bookings
   - Keep the old endpoint working for backward compatibility

3. **Phase 3**: Update the frontend to support the new multi-service booking flow

## Database Changes Required

```sql
-- Create AppointmentGroups table
CREATE TABLE AppointmentGroups (
    Id SERIAL PRIMARY KEY,
    ClientId INT NOT NULL,
    BookingDate TIMESTAMPTZ NOT NULL,
    Status VARCHAR(50) NOT NULL DEFAULT 'Confirmed',
    TotalPrice DECIMAL(10, 2) NOT NULL,
    TotalDuration INT NOT NULL,
    
    FOREIGN KEY (ClientId) REFERENCES Clients(Id)
);

-- Alter existing Appointments table to add group reference
ALTER TABLE Appointments 
ADD COLUMN AppointmentGroupId INT NULL;

-- Add foreign key constraint
ALTER TABLE Appointments
ADD CONSTRAINT FK_Appointments_AppointmentGroups
FOREIGN KEY (AppointmentGroupId) REFERENCES AppointmentGroups(Id);

-- Create indexes
CREATE INDEX idx_appointments_groupid ON Appointments(AppointmentGroupId);
CREATE INDEX idx_appointmentgroups_clientid ON AppointmentGroups(ClientId);
```

## API Changes Required

The `CreateAppointmentDto` would need to be updated to support multiple services:

```csharp
public class CreateMultiServiceAppointmentDto
{
    public List<int> ServiceIds { get; set; } = new List<int>();
    public DateTimeOffset StartTime { get; set; }
    public ClientDto? Client { get; set; }
}
```

## Conclusion

The current data model does not support multi-service appointments. The recommended approach is Option 3, which provides a gradual migration path while maintaining backward compatibility with existing code and minimizing the risk of service disruption.
