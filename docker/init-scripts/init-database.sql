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
    CategoryId INT,                     -- Nullable because ON DELETE SET NULL
    Name VARCHAR(255) NOT NULL,         -- Updated to match entity StringLength(255)
    Description TEXT,
    Duration INT,                       -- Duration in minutes
    Price DECIMAL(10, 2),               -- Price can be NULL for items like brands
    AppointmentBufferTime INT,          -- Number of weeks until next appointment for rescheduling
    Website VARCHAR(2048),              -- Updated to match entity StringLength(2048)
    AfterCareInstructions TEXT,         -- Aftercare instructions for post-appointment client emails

    -- Foreign key constraint
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
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

-- Part 3: Create ContactSubmissions table for contact form submissions

-- Create the ContactSubmissions table for PostgreSQL
CREATE TABLE IF NOT EXISTS ContactSubmissions (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    Phone VARCHAR(30),
    Subject VARCHAR(100) NOT NULL,
    Message TEXT NOT NULL,
    InterestedService VARCHAR(100), -- Optional
    PreferredContactMethod VARCHAR(50) DEFAULT 'Email',
    SubmittedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    Status VARCHAR(20) NOT NULL DEFAULT 'unread' CHECK (Status IN ('unread', 'read', 'responded')),
    ReadAt TIMESTAMPTZ,
    RespondedAt TIMESTAMPTZ,
    AdminNotes TEXT
);

-- Add indexes for ContactSubmissions table
CREATE INDEX IF NOT EXISTS idx_contactsubmissions_submittedat ON ContactSubmissions (SubmittedAt);
CREATE INDEX IF NOT EXISTS idx_contactsubmissions_status ON ContactSubmissions (Status);
CREATE INDEX IF NOT EXISTS idx_contactsubmissions_email ON ContactSubmissions (Email);
CREATE INDEX IF NOT EXISTS idx_contactsubmissions_interestedservice ON ContactSubmissions (InterestedService);

-- Part 3: Create GalleryImages table for admin gallery management

-- Create the GalleryImages table for PostgreSQL
CREATE TABLE IF NOT EXISTS GalleryImages (
    Id SERIAL PRIMARY KEY,              -- Use SERIAL for auto-incrementing PK in PostgreSQL
    Src VARCHAR(500) NOT NULL,          -- Image source URL
    Alt VARCHAR(255) NOT NULL,          -- Alt text for accessibility
    Category VARCHAR(50) NOT NULL,      -- Category for filtering (e.g., 'facial', 'before-after', 'facility')
    Title VARCHAR(255),                 -- Optional title
    Description VARCHAR(1000),          -- Optional description
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,  -- Whether image is publicly visible
    SortOrder INT NOT NULL DEFAULT 0,   -- Order for display
    UploadedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),  -- Upload timestamp
    UpdatedAt TIMESTAMPTZ               -- Last update timestamp
);

-- Add indexes for GalleryImages table
CREATE INDEX IF NOT EXISTS idx_galleryimages_category ON GalleryImages (Category);
CREATE INDEX IF NOT EXISTS idx_galleryimages_isactive ON GalleryImages (IsActive);
CREATE INDEX IF NOT EXISTS idx_galleryimages_sortorder ON GalleryImages (SortOrder);
CREATE INDEX IF NOT EXISTS idx_galleryimages_uploadedat ON GalleryImages (UploadedAt);

-- Part 4: Seed data from InitialSetup.sql

-- Seed Categories
INSERT INTO Categories (Id, Name) VALUES
(1, 'Facial Treatments'),
(2, 'Waxing'),
(3, 'Addons'),
(4, 'Skincare Brands I Use')
ON CONFLICT (Id) DO NOTHING; -- Avoid errors if IDs already exist

-- Seed Services
INSERT INTO Services (Id, CategoryId, Name, Description, Duration, Price, AppointmentBufferTime, Website, AfterCareInstructions) VALUES
-- Facial Treatments (CategoryId: 1)
(101, 1, 'Signature Facial', 'A personalized facial treatment tailored to your specific skin needs and concerns.', 60, 95.00, 0, NULL, 'Avoid direct sunlight for 24 hours. Use a gentle cleanser and moisturizer. Apply SPF 30+ daily.'),
(102, 1, 'Dermaplane + mini facial', 'A dual treatment combining dermaplaning to remove dead skin cells and peach fuzz, followed by a mini facial to cleanse and hydrate the skin.', 60, 100.00, 0, NULL, 'Your skin may be slightly sensitive for 24-48 hours. Avoid exfoliating products, retinoids, and direct sun exposure. Use gentle skincare and SPF 30+.'), -- Assuming 60 min based on range
(103, 1, 'Back Facial', 'A specialized treatment for the back area, focusing on cleansing, exfoliating, and hydrating.', 60, 115.00, 0, NULL, 'Avoid tight clothing for 24 hours. Keep the area clean and dry. Apply the recommended moisturizer daily.'),

-- Waxing (CategoryId: 2)
(201, 2, 'Upper lip wax', 'Quick and precise removal of unwanted hair from the upper lip area.', 5, 15.00, 0, NULL, 'Avoid touching the area for 2-4 hours. Use aloe vera gel if irritation occurs. Avoid makeup on the area for 4-6 hours.'),
(202, 2, 'Eyebrow wax', 'Precise shaping and grooming of eyebrows for a clean, defined look.', 10, 20.00, 0, NULL, 'Apply ice if you experience swelling. Avoid makeup and touching the area for 2-4 hours. Use aloe vera for any redness.'),

-- Addons (CategoryId: 3)
(301, 3, 'Chemical peels', 'An exfoliating treatment that improves skin texture and tone for a more radiant complexion.', NULL, 15.00, 0, NULL, 'Do not pick or peel flaking skin. Use gentle cleanser and moisturizer only. Avoid sun exposure and use SPF 30+ for 1-2 weeks.'), -- Duration is 'Varies', so set to NULL

-- Skincare Brands (CategoryId: 4) - Note: Price is NULL
(401, 4, 'SkinCeuticals', 'A skincare brand known for its advanced skincare products backed by science.', NULL, NULL, 0, 'https://www.skinceuticals.com/', NULL),
(402, 4, 'Bioelements', 'A professional skincare brand offering customized skincare solutions for all skin types.', NULL, NULL, 0, 'https://www.bioelements.com/', NULL)
ON CONFLICT (Id) DO NOTHING; -- Avoid errors if IDs already exist

-- Part 5: Seed test data for client review system testing
-- Test clients with different review states for comprehensive testing:
-- ID 7 (Sarah Johnson): Has pending review flag
-- ID 8 (Emma Davis): Has approved review flag  
-- ID 10 (Lisa Brown): No review flags
-- ID 11 (Michael Wilson): Has rejected review flag
-- ID 12 (Marcus Thompson): Has banned review flags (multiple)
-- ID 15 (David Anderson): No review flags - clean state

-- Seed test clients
INSERT INTO Clients (Id, FirstName, LastName, Email, PhoneNumber) VALUES
(2, 'John', 'Doe', 'john.doe@email.com', '555-0101'),
(3, 'Jane', 'Smith', 'jane.smith@email.com', '555-0102'),
(4, 'Mike', 'Johnson', 'mike.johnson@email.com', '555-0103'),
(5, 'Sarah', 'Wilson', 'sarah.wilson@email.com', '555-0104'),
(6, 'Bob', 'Brown', 'bob.brown@email.com', '555-0105'),
(7, 'Sarah', 'Johnson', 'sarah.johnson@email.com', '555-123-4567'),
(8, 'Emma', 'Davis', 'emma.davis@email.com', '555-987-6543'),
(10, 'Lisa', 'Brown', 'lisa.brown@email.com', '555-321-0987'),
(11, 'Michael', 'Wilson', 'michael.wilson@email.com', '555-456-7890'),
-- Additional test clients for different review states
(12, 'Marcus', 'Thompson', 'marcus.thompson@email.com', '555-000-0001'),
(15, 'David', 'Anderson', 'david.anderson@email.com', '555-000-0004')
ON CONFLICT (Id) DO NOTHING;

-- Seed test appointments
INSERT INTO Appointments (Id, ClientId, ServiceId, Time) VALUES
(2, 7, 101, '2025-06-15 10:00:00+00'),
(3, 7, 102, '2025-06-15 14:00:00+00'),
(4, 8, 201, '2025-06-16 11:00:00+00'),
(5, 8, 202, '2025-06-16 11:30:00+00'),
(7, 10, 102, '2025-06-18 13:00:00+00'),
(8, 11, 101, '2025-06-20 10:00:00+00'),
(9, 11, 202, '2025-06-20 14:00:00+00'),
-- Additional appointments for test clients with different review states
(10, 12, 101, '2025-06-17 09:00:00+00'), -- Marcus Thompson appointment
(11, 12, 201, '2025-06-17 10:00:00+00'), -- Marcus Thompson second appointment
(14, 15, 201, '2025-06-22 16:00:00+00')  -- Clean client appointment
ON CONFLICT (Id) DO NOTHING;

-- Seed test client review flags
INSERT INTO ClientReviewFlags (Id, ClientId, AppointmentId, FlagReason, FlagDate, ReviewedBy, ReviewDate, Status, AdminComments, AutoFlags) VALUES
-- Existing test data
(2, 7, 3, 'Multiple bookings detected: Client attempted to book 2 appointments on 2025-06-15', '2025-06-10 14:23:34.120134+00', NULL, NULL, 'Pending', NULL, 1),
(3, 8, 5, 'Multiple bookings detected: Client attempted to book 2 appointments on 2025-06-16', '2025-06-10 14:23:58.077211+00', 'Admin', '2025-06-10 14:34:00.317465+00', 'Approved', 'We talked and she is ok', 1),
(7, 11, 9, 'Multiple bookings detected: Client attempted to book 2 appointments on 2025-06-20', '2025-06-10 14:45:32.482941+00', 'Admin', '2025-06-10 15:02:31.89886+00', 'Rejected', 'felt mean', 1),
-- Additional test data for different review states
(10, 12, 10, 'Inappropriate behavior during previous appointment', '2025-06-09 10:00:00+00', 'Admin', '2025-06-09 10:30:00+00', 'Banned', 'Client was rude to staff and refused to follow safety protocols', 1)
ON CONFLICT (Id) DO NOTHING;

-- Seed test contact submissions
INSERT INTO ContactSubmissions (Id, Name, Email, Phone, Subject, Message, InterestedService, PreferredContactMethod, SubmittedAt, Status, ReadAt, RespondedAt, AdminNotes) VALUES
-- Unread submissions (recent)
(1, 'Jessica Williams', 'jessica.williams@email.com', '555-101-2345', 'Facial Treatment Inquiry', 'Hi! I''m interested in learning more about your signature facial treatments. I have sensitive skin and would like to know what options you recommend. Also, do you offer consultations?', 'Signature Facial', 'Email', '2025-06-10 09:30:00+00', 'unread', NULL, NULL, NULL),

(2, 'Robert Chen', 'robert.chen@gmail.com', '555-202-3456', 'Booking Question', 'Hello, I''m trying to book an appointment for my wife for a dermaplane treatment. What''s your availability like for next week? She prefers morning appointments if possible.', 'Dermaplane + mini facial', 'Phone', '2025-06-10 11:15:00+00', 'unread', NULL, NULL, NULL),

(3, 'Amanda Rodriguez', 'amanda.r.wellness@yahoo.com', NULL, 'Skincare Consultation', 'I''ve been struggling with acne for years and heard great things about your treatments. Could we schedule a consultation to discuss the best approach for my skin type? I''m particularly interested in chemical peels.', 'Chemical peels', 'Email', '2025-06-09 16:45:00+00', 'unread', NULL, NULL, NULL),

-- Read but not responded
(4, 'Michael Thompson', 'mthompson.business@outlook.com', '555-303-4567', 'Corporate Wellness Program', 'We''re looking to set up a corporate wellness program for our employees and would like to include facial treatments. Could you provide information about group bookings and potential discounts for bulk appointments?', NULL, 'Email', '2025-06-08 14:20:00+00', 'read', '2025-06-09 08:30:00+00', NULL, 'Interesting opportunity - need to discuss pricing structure'),

(5, 'Sophie Turner', 'sophie.turner@email.com', '555-404-5678', 'Wedding Prep Services', 'I''m getting married in 3 months and want to start a skincare routine to look my best. What would you recommend for bridal prep? I''m thinking about regular facials leading up to the big day.', 'Signature Facial', 'Phone', '2025-06-07 10:30:00+00', 'read', '2025-06-08 09:15:00+00', NULL, 'Perfect candidate for our bridal package - follow up needed'),

-- Responded submissions
(6, 'Carlos Martinez', 'carlos.martinez@email.com', '555-505-6789', 'Back Facial Questions', 'I work out regularly and have been getting breakouts on my back. Do your back facial treatments help with this? What''s the process like and how many sessions would I need?', 'Back Facial', 'Email', '2025-06-05 13:45:00+00', 'responded', '2025-06-05 15:20:00+00', '2025-06-06 10:30:00+00', 'Responded with detailed treatment plan and pricing. Scheduled consultation for June 12th.'),

(7, 'Patricia Davis', 'patricia.davis@email.com', '555-606-7890', 'Product Recommendations', 'After my recent facial, you mentioned some SkinCeuticals products that would be good for my skin. Could you email me the specific product names and where I can purchase them?', NULL, 'Email', '2025-06-03 11:20:00+00', 'responded', '2025-06-03 14:15:00+00', '2025-06-03 16:45:00+00', 'Sent product recommendations and provided purchase links. Client very satisfied.'),

(8, 'Daniel Kim', 'daniel.kim@email.com', '555-707-8901', 'Men''s Skincare Services', 'Do you offer services specifically designed for men? I''m new to professional skincare and not sure what would be appropriate. Any guidance would be appreciated.', 'Signature Facial', 'Phone', '2025-06-01 09:15:00+00', 'responded', '2025-06-01 11:30:00+00', '2025-06-01 14:20:00+00', 'Explained our gender-neutral approach and customized treatments. Booked appointment for June 15th.'),

-- Older submissions for historical data
(9, 'Linda Wilson', 'linda.wilson@email.com', '555-808-9012', 'Eyebrow Waxing Appointment', 'I''d like to schedule regular eyebrow waxing appointments. What''s your availability like and do you offer any packages for regular clients?', 'Eyebrow wax', 'Email', '2025-05-28 16:30:00+00', 'responded', '2025-05-29 09:00:00+00', '2025-05-29 11:15:00+00', 'Set up monthly standing appointment. Client very happy with service.'),

(10, 'James Rodriguez', 'james.rodriguez@email.com', '555-909-0123', 'Gift Certificate Inquiry', 'I want to buy a gift certificate for my mother''s birthday. She loves facials and I think she''d really enjoy your signature treatment. How do I go about purchasing one?', 'Signature Facial', 'Phone', '2025-05-25 12:45:00+00', 'responded', '2025-05-26 08:20:00+00', '2025-05-26 10:30:00+00', 'Helped set up gift certificate. Mother booked appointment and loved the service!')
ON CONFLICT (Id) DO NOTHING;

-- Part 7: Seed sample gallery images using actual available images for testing admin gallery functionality
-- Using correct MinIO object keys (these will be converted to proper URLs by the storage service)

INSERT INTO GalleryImages (Id, Src, Alt, Category, Title, Description, IsActive, SortOrder, UploadedAt) VALUES
-- Facial treatment images using MinIO object keys (no leading slash, no full URLs)
(1, 'images/gallery/facial-treatment-1.jpg', 'Professional facial treatment session', 'facials', 'Signature Facial Treatment', 'Our signature facial treatment showcasing professional skincare techniques and relaxing environment.', true, 1, '2025-01-01 10:00:00+00'),
(2, 'images/gallery/facial-treatment-2.jpg', 'Relaxing skincare treatment', 'facials', 'Custom Facial Experience', 'Customized facial treatment tailored to individual skin needs and concerns.', true, 2, '2025-01-01 11:00:00+00'),
(3, 'images/gallery/dermaplaning-treatment.jpg', 'Advanced skincare procedure', 'facials', 'Dermaplaning Treatment', 'Professional dermaplaning service for smooth, radiant skin results.', true, 3, '2025-01-01 12:00:00+00'),

-- Before and after results
(4, 'images/gallery/before-after-facial.jpg', 'Before and after facial treatment results', 'before-after', 'Facial Treatment Results', 'Dramatic improvement in skin clarity and texture after our signature facial treatments.', true, 1, '2025-01-02 10:00:00+00'),
(5, 'images/gallery/skincare-transformation.jpg', 'Before and after skincare transformation', 'before-after', 'Skincare Transformation', 'Visible improvement in skin health and appearance after customized treatment plan.', true, 2, '2025-01-02 11:00:00+00'),

-- Studio and facility photos
(6, 'images/gallery/treatment-room.jpg', 'Professional treatment room setup', 'studio', 'Treatment Room', 'Our peaceful and professional treatment rooms designed for ultimate client comfort.', true, 1, '2025-01-03 10:00:00+00'),
(7, 'images/gallery/studio-atmosphere.jpg', 'Studio mascot and ambiance', 'studio', 'Studio Atmosphere', 'The welcoming and relaxing atmosphere that makes our studio special.', true, 2, '2025-01-03 11:00:00+00'),

-- Waxing services
(8, 'images/gallery/eyebrow-waxing.jpg', 'Professional waxing service', 'waxing', 'Eyebrow Waxing', 'Expert eyebrow shaping and waxing services for perfectly defined brows.', true, 1, '2025-01-04 10:00:00+00'),
(9, 'images/gallery/professional-waxing.jpg', 'Precision waxing technique', 'waxing', 'Professional Waxing', 'Clean, hygienic waxing environment with professional techniques and care.', true, 2, '2025-01-04 11:00:00+00'),

-- Body treatment examples
(10, 'images/gallery/body-treatment.jpg', 'Relaxing body treatment session', 'body', 'Body Treatment', 'Comprehensive body treatments designed to rejuvenate and refresh.', true, 1, '2025-01-05 10:00:00+00'),
(11, 'images/gallery/body-care-services.jpg', 'Professional body care service', 'body', 'Body Care Services', 'Expert body treatments using premium products and techniques.', true, 2, '2025-01-05 11:00:00+00'),

-- Products showcase
(12, 'images/gallery/premium-skincare.jpg', 'Professional skincare products', 'products', 'Premium Skincare', 'High-quality skincare products from trusted brands like SkinCeuticals and Bioelements.', true, 1, '2025-01-06 10:00:00+00'),
(13, 'images/gallery/product-education.jpg', 'Product consultation session', 'products', 'Product Education', 'Learning about proper skincare routines and product application techniques.', true, 2, '2025-01-06 11:00:00+00'),

-- Makeup services (using available images)
(14, 'images/gallery/makeup-services.jpg', 'Professional makeup application', 'makeup', 'Makeup Services', 'Expert makeup application for special occasions and everyday looks.', true, 1, '2025-01-07 10:00:00+00'),
(15, 'images/gallery/bridal-makeup.jpg', 'Bridal makeup preparation', 'makeup', 'Bridal Makeup', 'Specialized bridal makeup services for your perfect wedding day look.', false, 2, '2025-01-07 11:00:00+00')
ON CONFLICT (Id) DO NOTHING;

-- Reset sequences to proper values
SELECT setval(pg_get_serial_sequence('categories', 'id'), (SELECT COALESCE(MAX(id), 1) FROM categories), true);
SELECT setval(pg_get_serial_sequence('services', 'id'), (SELECT COALESCE(MAX(id), 1) FROM services), true);
SELECT setval(pg_get_serial_sequence('clients', 'id'), (SELECT COALESCE(MAX(id), 1) FROM clients), true);
SELECT setval(pg_get_serial_sequence('appointments', 'id'), (SELECT COALESCE(MAX(id), 1) FROM appointments), true);
SELECT setval(pg_get_serial_sequence('clientreviewflags', 'id'), (SELECT COALESCE(MAX(id), 1) FROM clientreviewflags), true);
SELECT setval(pg_get_serial_sequence('contactsubmissions', 'id'), (SELECT COALESCE(MAX(id), 1) FROM contactsubmissions), true);
SELECT setval(pg_get_serial_sequence('galleryimages', 'id'), (SELECT COALESCE(MAX(id), 1) FROM galleryimages), true);
