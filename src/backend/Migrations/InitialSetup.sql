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
    Duration INT,                       -- Duration, e.g., in minutes
    Price DECIMAL(10, 2) NOT NULL,
    Website VARCHAR(255),

    -- Foreign key constraint remains the same
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
        -- ON DELETE SET NULL -- Optional action
        -- ON DELETE CASCADE -- Optional action
);