# 🗃️ Database Query Guide - Esthetics by Elizabeth

This guide provides useful PostgreSQL commands to query and inspect data in the Esthetics by Elizabeth database. Use these commands to verify API functionality and troubleshoot data issues.

## 🔗 Database Connection

The PostgreSQL database runs in Docker with the following connection details:

```bash
# Connection Details
Host: localhost
Port: 5432
Database: postgres
Username: postgres
Password: Ieatbugsandsquashfrogs
```

### Connect to Database

```bash
# Option 1: Connect via Docker exec
docker exec -it local-postgres psql -U postgres -d postgres

# Option 2: Connect from host (if psql is installed)
psql -h localhost -p 5432 -U postgres -d postgres
```

## 📊 Database Schema Overview

The database contains the following main tables:

- **`clients`** - Customer information
- **`categories`** - Service categories
- **`services`** - Available treatments/services
- **`appointments`** - Scheduled appointments
- **`clientreviewflags`** - Client review/ban system

## 🔍 Basic Data Inspection Queries

### View All Tables
```sql
-- List all tables
\dt

-- Get table details with row counts
SELECT 
    schemaname,
    tablename,
    attname,
    typename,
    attnum
FROM pg_tables t
JOIN pg_attribute a ON a.attrelid = (SELECT oid FROM pg_class WHERE relname = t.tablename)
JOIN pg_type ty ON ty.oid = a.atttypid
WHERE t.schemaname = 'public'
ORDER BY tablename, attnum;
```

### Quick Data Overview
```sql
-- Get row counts for all tables
SELECT 
    'clients' as table_name, COUNT(*) as row_count FROM clients
UNION ALL
SELECT 'categories', COUNT(*) FROM categories
UNION ALL
SELECT 'services', COUNT(*) FROM services
UNION ALL
SELECT 'appointments', COUNT(*) FROM appointments
UNION ALL
SELECT 'clientreviewflags', COUNT(*) FROM clientreviewflags;
```

## 🎯 Function-Specific Queries

### 1. GetServiceList Function
**Purpose**: Returns all services with their categories

```sql
-- Query what GetServiceList returns
SELECT 
    s.id,
    s.name,
    s.description,
    s.duration,
    s.price,
    s.website,
    c.name as category_name,
    c.id as category_id
FROM services s
LEFT JOIN categories c ON s.categoryid = c.id
ORDER BY c.name, s.name;

-- Verify service data by category
SELECT 
    c.name as category,
    COUNT(s.id) as service_count,
    AVG(s.price) as avg_price,
    AVG(s.duration) as avg_duration
FROM categories c
LEFT JOIN services s ON c.id = s.categoryid
GROUP BY c.id, c.name
ORDER BY c.name;
```

### 2. GetCategories Function
**Purpose**: Returns all service categories

```sql
-- Query what GetCategories returns
SELECT id, name FROM categories ORDER BY name;

-- Verify categories have services
SELECT 
    c.id,
    c.name,
    COUNT(s.id) as service_count
FROM categories c
LEFT JOIN services s ON c.id = s.categoryid
GROUP BY c.id, c.name
ORDER BY c.name;
```

### 3. ScheduleAppointment Function
**Purpose**: Creates new appointments

```sql
-- View recent appointments (what gets added)
SELECT 
    a.id,
    a.time,
    c.firstname,
    c.lastname,
    c.email,
    s.name as service_name,
    s.duration,
    s.price
FROM appointments a
JOIN clients c ON a.clientid = c.id
JOIN services s ON a.serviceid = s.id
ORDER BY a.time DESC
LIMIT 10;

-- Check for duplicate bookings (validation logic)
SELECT 
    c.email,
    DATE(a.time) as appointment_date,
    COUNT(*) as bookings_same_day
FROM appointments a
JOIN clients c ON a.clientid = c.id
GROUP BY c.email, DATE(a.time)
HAVING COUNT(*) > 1
ORDER BY appointment_date DESC;

-- Find clients under review (blocks booking)
SELECT 
    c.id,
    c.email,
    crf.status,
    crf.flagreason,
    crf.flagdate
FROM clients c
JOIN clientreviewflags crf ON c.id = crf.clientid
WHERE crf.status IN ('Pending', 'Banned')
ORDER BY crf.flagdate DESC;
```

### 4. GetAppointmentsByDate Function
**Purpose**: Returns appointments for a specific date

```sql
-- Query appointments for a specific date (replace date as needed)
SELECT 
    a.id,
    a.time,
    c.firstname,
    c.lastname,
    c.email,
    c.phonenumber,
    s.name as service_name,
    s.duration,
    s.price
FROM appointments a
JOIN clients c ON a.clientid = c.id
JOIN services s ON a.serviceid = s.id
WHERE DATE(a.time) = '2025-06-07'  -- Replace with desired date
ORDER BY a.time;

-- Get appointments for current date
SELECT 
    a.id,
    TO_CHAR(a.time, 'HH24:MI') as appointment_time,
    c.firstname || ' ' || c.lastname as client_name,
    s.name as service_name,
    s.duration || ' min' as duration
FROM appointments a
JOIN clients c ON a.clientid = c.id
JOIN services s ON a.serviceid = s.id
WHERE DATE(a.time) = CURRENT_DATE
ORDER BY a.time;

-- Check availability for a date range
SELECT 
    DATE(a.time) as date,
    COUNT(*) as total_appointments,
    SUM(s.duration) as total_minutes,
    ROUND(SUM(s.duration)/60.0, 1) as total_hours
FROM appointments a
JOIN services s ON a.serviceid = s.id
WHERE a.time >= CURRENT_DATE 
AND a.time < CURRENT_DATE + INTERVAL '7 days'
GROUP BY DATE(a.time)
ORDER BY date;
```

### 5. GetAppointmentHistoryByClient Function
**Purpose**: Returns appointment history for a client

```sql
-- Query appointment history for a specific client
SELECT 
    a.id,
    a.time,
    s.name as service_name,
    s.duration,
    s.price,
    c.firstname,
    c.lastname,
    c.email
FROM appointments a
JOIN clients c ON a.clientid = c.id
JOIN services s ON a.serviceid = s.id
WHERE c.email = 'client@example.com'  -- Replace with actual email
ORDER BY a.time DESC;

-- Find clients with most appointments
SELECT 
    c.id,
    c.firstname,
    c.lastname,
    c.email,
    COUNT(a.id) as total_appointments,
    SUM(s.price) as total_spent,
    MIN(a.time) as first_appointment,
    MAX(a.time) as last_appointment
FROM clients c
LEFT JOIN appointments a ON c.id = a.clientid
LEFT JOIN services s ON a.serviceid = s.id
GROUP BY c.id, c.firstname, c.lastname, c.email
HAVING COUNT(a.id) > 0
ORDER BY total_appointments DESC;
```

### 6. CancelAppointment Function
**Purpose**: Deletes appointments

```sql
-- View recent appointment deletions (check logs or audit trail)
-- Note: Since this is a DELETE operation, you'd need to check before deletion

-- Find appointments that might be cancelled (past due, etc.)
SELECT 
    a.id,
    a.time,
    c.firstname || ' ' || c.lastname as client_name,
    c.email,
    s.name as service_name,
    CASE 
        WHEN a.time < NOW() THEN 'Past Due'
        WHEN a.time < NOW() + INTERVAL '24 hours' THEN 'Within 24h'
        ELSE 'Future'
    END as status
FROM appointments a
JOIN clients c ON a.clientid = c.id
JOIN services s ON a.serviceid = s.id
ORDER BY a.time;

-- Check for appointments by ID (before cancellation)
SELECT 
    a.id,
    a.time,
    c.firstname,
    c.lastname,
    c.email,
    s.name as service_name
FROM appointments a
JOIN clients c ON a.clientid = c.id
JOIN services s ON a.serviceid = s.id
WHERE a.id = 123;  -- Replace with actual appointment ID
```

### 7. UnbanClient Function
**Purpose**: Removes client bans

```sql
-- View client review flags (what gets modified)
SELECT 
    crf.id,
    c.email,
    crf.status,
    crf.flagreason,
    crf.flagdate,
    crf.reviewedby,
    crf.reviewdate,
    crf.admincomments,
    crf.autoflags
FROM clientreviewflags crf
JOIN clients c ON crf.clientid = c.id
ORDER BY crf.flagdate DESC;

-- Find banned clients
SELECT 
    c.id,
    c.email,
    c.firstname,
    c.lastname,
    COUNT(crf.id) as total_flags,
    MAX(crf.flagdate) as latest_flag
FROM clients c
JOIN clientreviewflags crf ON c.id = crf.clientid
WHERE crf.status = 'Banned'
GROUP BY c.id, c.email, c.firstname, c.lastname
ORDER BY latest_flag DESC;

-- Check client status before unbanning
SELECT 
    c.id,
    c.email,
    crf.status,
    crf.flagreason,
    crf.admincomments
FROM clients c
JOIN clientreviewflags crf ON c.id = crf.clientid
WHERE c.id = 123  -- Replace with actual client ID
ORDER BY crf.flagdate DESC;
```

## 🧪 Testing Data Validation

### Verify Data Integrity
```sql
-- Check for orphaned records
SELECT 'Appointments with invalid ClientId' as issue, COUNT(*) 
FROM appointments a 
LEFT JOIN clients c ON a.clientid = c.id 
WHERE c.id IS NULL

UNION ALL

SELECT 'Appointments with invalid ServiceId', COUNT(*) 
FROM appointments a 
LEFT JOIN services s ON a.serviceid = s.id 
WHERE s.id IS NULL

UNION ALL

SELECT 'Services with invalid CategoryId', COUNT(*) 
FROM services s 
LEFT JOIN categories c ON s.categoryid = c.id 
WHERE s.categoryid IS NOT NULL AND c.id IS NULL;

-- Validate email formats
SELECT 
    id,
    email,
    firstname,
    lastname
FROM clients 
WHERE email !~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'
ORDER BY id;

-- Check for future appointments
SELECT 
    COUNT(*) as future_appointments,
    MIN(time) as next_appointment,
    MAX(time) as furthest_appointment
FROM appointments 
WHERE time > NOW();
```

## 🚀 Performance Monitoring

### Index Usage
```sql
-- Check index usage
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_tup_read,
    idx_tup_fetch,
    idx_scan
FROM pg_stat_user_indexes 
ORDER BY idx_scan DESC;

-- Slow queries analysis
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    rows
FROM pg_stat_statements 
ORDER BY mean_time DESC 
LIMIT 10;
```

### Query Performance
```sql
-- Most active tables
SELECT 
    schemaname,
    tablename,
    seq_scan,
    seq_tup_read,
    idx_scan,
    idx_tup_fetch,
    n_tup_ins,
    n_tup_upd,
    n_tup_del
FROM pg_stat_user_tables 
ORDER BY seq_scan + idx_scan DESC;
```

## 📝 Sample Test Data

### Insert Test Data
```sql
-- Add a test client
INSERT INTO clients (firstname, lastname, email, phonenumber) 
VALUES ('John', 'Doe', 'john.doe@test.com', '555-1234');

-- Add a test appointment
INSERT INTO appointments (clientid, serviceid, time) 
VALUES (
    (SELECT id FROM clients WHERE email = 'john.doe@test.com'),
    101,  -- Signature Facial
    '2025-06-08 14:00:00+00'
);

-- Add a test review flag
INSERT INTO clientreviewflags (clientid, appointmentid, flagreason, flagdate, status) 
VALUES (
    (SELECT id FROM clients WHERE email = 'john.doe@test.com'),
    (SELECT id FROM appointments WHERE clientid = (SELECT id FROM clients WHERE email = 'john.doe@test.com') LIMIT 1),
    'Multiple bookings on same day',
    NOW(),
    'Pending'
);
```

### Clean Test Data
```sql
-- Remove test data (use carefully!)
DELETE FROM clientreviewflags WHERE clientid IN (SELECT id FROM clients WHERE email LIKE '%@test.com');
DELETE FROM appointments WHERE clientid IN (SELECT id FROM clients WHERE email LIKE '%@test.com');
DELETE FROM clients WHERE email LIKE '%@test.com';
```

## 🛠️ Useful Utility Commands

### Database Maintenance
```sql
-- Vacuum and analyze
VACUUM ANALYZE;

-- Check database size
SELECT 
    pg_size_pretty(pg_database_size('postgres')) as database_size;

-- Check table sizes
SELECT 
    tablename,
    pg_size_pretty(pg_total_relation_size(tablename::regclass)) as size
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(tablename::regclass) DESC;
```

### Export Data
```sql
-- Export to CSV (run from psql)
\copy (SELECT a.id, a.time, c.email, s.name FROM appointments a JOIN clients c ON a.clientid = c.id JOIN services s ON a.serviceid = s.id) TO '/tmp/appointments.csv' WITH CSV HEADER;
```

---

**💡 Pro Tips:**
- Always test queries on development data first
- Use `LIMIT` for large result sets
- Remember to escape single quotes in strings with `''`
- Use `EXPLAIN ANALYZE` to optimize slow queries
- The database resets when containers are recreated unless data is persisted

**🚨 Safety Reminders:**
- Never run `DELETE` or `UPDATE` without `WHERE` clauses in production
- Always backup before making schema changes
- Test API functions after manual data modifications
