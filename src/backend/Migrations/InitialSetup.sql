-- Create the Clients table for PostgreSQL
CREATE TABLE Clients (
    Id SERIAL PRIMARY KEY,              -- Use SERIAL for auto-incrementing PK in PostgreSQL
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(255) UNIQUE,
    PhoneNumber VARCHAR(30)
);

-- Create the Categories table for PostgreSQL
CREATE TABLE Categories (
    Id SERIAL PRIMARY KEY,              -- Use SERIAL for auto-incrementing PK in PostgreSQL
    Name VARCHAR(150) NOT NULL UNIQUE
);

-- Create the Services table for PostgreSQL
CREATE TABLE Services (
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
CREATE TABLE Appointments (
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

-- Optional: Index for faster lookups based on client email if frequently used
-- CREATE INDEX IF NOT EXISTS idx_clients_email ON Clients (Email);


-- Seed Data --

-- Seed Categories (Using specific IDs from frontend for consistency, adjust if needed)
-- Note: Using explicit IDs requires careful management if SERIAL is also used.
-- Consider removing SERIAL if you always want to control IDs via INSERTs.
-- Or, let SERIAL manage IDs and adjust Service INSERTs accordingly.
-- For this example, we'll assume we want to match the frontend IDs.
-- We need to temporarily allow inserting specific values into SERIAL columns or adjust the table definition.
-- A common approach is to use SETVAL after inserts if needed, or define IDs explicitly without SERIAL.
-- Let's stick to explicit IDs for now, assuming manual control or later sequence adjustment.

-- Temporarily adjust sequence for explicit ID insertion if needed (run separately or handle potential conflicts)
-- SELECT setval(pg_get_serial_sequence('categories', 'id'), COALESCE(max(id), 1)) FROM categories;
-- SELECT setval(pg_get_serial_sequence('services', 'id'), COALESCE(max(id), 1)) FROM services;

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

-- Reset sequences after explicit ID inserts if needed (run separately)
-- SELECT setval(pg_get_serial_sequence('categories', 'id'), COALESCE(max(id), 1)) FROM categories;
-- SELECT setval(pg_get_serial_sequence('services', 'id'), COALESCE(max(id), 1)) FROM services;


-- command to run locally: cat InitialSetup.sql | docker exec -i local-postgres psql -U postgres -d postgres
-- command to run in docker run --name local-postgres -e POSTGRES_PASSWORD=Ieatbugsandsquashfrogs -p 5432:5432 -d postgres < InitialSetup.sql
-- command to start pgadmin docker run --name my-pgadmin     -p 5050:80     -e PGADMIN_DEFAULT_EMAIL="admin@test.com"     -e PGADMIN_DEFAULT_PASSWORD="password123"     -d     dpage/pgadmin4