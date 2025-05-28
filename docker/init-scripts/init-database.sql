-- Combined SQL initialization script
-- Part 1: Initial database setup from InitialSetup.sql

-- Create the Clients table for PostgreSQL
CREATE TABLE IF NOT EXISTS Clients (
    Id SERIAL PRIMARY KEY,              -- Use SERIAL for auto-incrementing PK in PostgreSQL
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(255) UNIQUE,
    PhoneNumber VARCHAR(30)
);

-- Create the Categories table for PostgreSQL
CREATE TABLE IF NOT EXISTS Categories (
    Id SERIAL PRIMARY KEY,              -- Use SERIAL for auto-incrementing PK in PostgreSQL
    Name VARCHAR(150) NOT NULL UNIQUE
);

-- Create the Services table for PostgreSQL
CREATE TABLE IF NOT EXISTS Services (
    Id SERIAL PRIMARY KEY,              -- Use SERIAL for auto-incrementing PK in PostgreSQL
    CategoryId INT NOT NULL,
    Name VARCHAR(150) NOT NULL,
    Description TEXT,
    Duration INT,                       -- Duration in minutes
    Price DECIMAL(10, 2),               -- Price can be NULL for items like brands
    Website VARCHAR(255),

    -- Foreign key constraint
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

-- Create the Appointments table for PostgreSQL
CREATE TABLE IF NOT EXISTS Appointments (
    Id SERIAL PRIMARY KEY,
    ClientId INT NOT NULL,
    ServiceId INT NOT NULL,
    Time TIMESTAMPTZ NOT NULL,

    -- Foreign key constraints
    FOREIGN KEY (ClientId) REFERENCES Clients(Id),
    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);

-- Add Indexes for Foreign Keys and frequently queried columns

-- Index for faster lookups/joins based on CategoryId in Services
CREATE INDEX IF NOT EXISTS idx_services_categoryid ON Services (CategoryId);

-- Index for faster lookups/joins based on ClientId in Appointments
CREATE INDEX IF NOT EXISTS idx_appointments_clientid ON Appointments (ClientId);

-- Index for faster lookups/joins based on ServiceId in Appointments
CREATE INDEX IF NOT EXISTS idx_appointments_serviceid ON Appointments (ServiceId);

-- Index for faster lookups based on appointment time (important for availability checks)
CREATE INDEX IF NOT EXISTS idx_appointments_time ON Appointments (Time);

-- Part 2: Create ClientReviewFlags table from add-client-review-flags-table.sh

-- Create the ClientReviewFlags table for PostgreSQL
CREATE TABLE IF NOT EXISTS ClientReviewFlags (
    Id SERIAL PRIMARY KEY,              -- Use SERIAL for auto-incrementing PK in PostgreSQL
    ClientId INT NOT NULL,
    AppointmentId INT NOT NULL,
    FlagReason VARCHAR(500) NOT NULL,
    FlagDate TIMESTAMPTZ NOT NULL,
    ReviewedBy VARCHAR(255),            -- NULL if not yet reviewed
    ReviewDate TIMESTAMPTZ,             -- NULL if not yet reviewed
    Status VARCHAR(50) NOT NULL,        -- 'Pending', 'Approved', 'Rejected', 'Banned'
    AdminComments VARCHAR(1000),        -- NULL if no comments
    AutoFlags INT NOT NULL DEFAULT 1,   -- Number of times auto-flagged

    -- Foreign key constraints
    FOREIGN KEY (ClientId) REFERENCES Clients(Id),
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id)
);

-- Add index for faster lookups based on ClientId (important for frequent client checks)
CREATE INDEX IF NOT EXISTS idx_clientreviewflags_clientid ON ClientReviewFlags (ClientId);

-- Add index for faster lookups based on AppointmentId
CREATE INDEX IF NOT EXISTS idx_clientreviewflags_appointmentid ON ClientReviewFlags (AppointmentId);

-- Add index for faster filtering by status (important for admin review queries)
CREATE INDEX IF NOT EXISTS idx_clientreviewflags_status ON ClientReviewFlags (Status);

-- Add index for timestamp-based sorting (if admins will often sort by date)
CREATE INDEX IF NOT EXISTS idx_clientreviewflags_flagdate ON ClientReviewFlags (FlagDate);

-- Part 3: Seed data from InitialSetup.sql

-- Seed Categories
INSERT INTO Categories (Id, Name) VALUES
(1, 'Facial Treatments'),
(2, 'Waxing'),
(3, 'Addons'),
(4, 'Skincare Brands I Use')
ON CONFLICT (Id) DO NOTHING; -- Avoid errors if IDs already exist

-- Seed Services
INSERT INTO Services (Id, CategoryId, Name, Description, Duration, Price, Website) VALUES
-- Facial Treatments (CategoryId: 1)
(101, 1, 'Signature Facial', 'A personalized facial treatment tailored to your specific skin needs and concerns.', 60, 95.00, NULL),
(102, 1, 'Dermaplane + mini facial', 'A dual treatment combining dermaplaning to remove dead skin cells and peach fuzz, followed by a mini facial to cleanse and hydrate the skin.', 60, 100.00, NULL), -- Assuming 60 min based on range
(103, 1, 'Back Facial', 'A specialized treatment for the back area, focusing on cleansing, exfoliating, and hydrating.', 60, 115.00, NULL),

-- Waxing (CategoryId: 2)
(201, 2, 'Upper lip wax', 'Quick and precise removal of unwanted hair from the upper lip area.', 5, 15.00, NULL),
(202, 2, 'Eyebrow wax', 'Precise shaping and grooming of eyebrows for a clean, defined look.', 10, 20.00, NULL),

-- Addons (CategoryId: 3)
(301, 3, 'Chemical peels', 'An exfoliating treatment that improves skin texture and tone for a more radiant complexion.', NULL, 15.00, NULL), -- Duration is 'Varies', so set to NULL

-- Skincare Brands (CategoryId: 4) - Note: Price is NULL
(401, 4, 'SkinCeuticals', 'A skincare brand known for its advanced skincare products backed by science.', NULL, NULL, 'https://www.skinceuticals.com/'),
(402, 4, 'Bioelements', 'A professional skincare brand offering customized skincare solutions for all skin types.', NULL, NULL, 'https://www.bioelements.com/')
ON CONFLICT (Id) DO NOTHING; -- Avoid errors if IDs already exist

-- Reset sequences to proper values
SELECT setval(pg_get_serial_sequence('categories', 'id'), (SELECT COALESCE(MAX(id), 1) FROM categories), true);
SELECT setval(pg_get_serial_sequence('services', 'id'), (SELECT COALESCE(MAX(id), 1) FROM services), true);
SELECT setval(pg_get_serial_sequence('clients', 'id'), (SELECT COALESCE(MAX(id), 1) FROM clients), true);
SELECT setval(pg_get_serial_sequence('appointments', 'id'), (SELECT COALESCE(MAX(id), 1) FROM appointments), true);
SELECT setval(pg_get_serial_sequence('clientreviewflags', 'id'), (SELECT COALESCE(MAX(id), 1) FROM clientreviewflags), true);
