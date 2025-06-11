-- Gallery Images Table Migration
-- Created: 2025-01-20
-- Description: Adds GalleryImages table for admin gallery management system

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

-- Reset sequence to proper value
SELECT setval(pg_get_serial_sequence('galleryimages', 'id'), (SELECT COALESCE(MAX(id), 1) FROM galleryimages), true);
