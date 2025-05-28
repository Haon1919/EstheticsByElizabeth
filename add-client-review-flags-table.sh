#!/bin/bash
# filepath: /home/noah/test/add-client-review-flags-table.sh
# Script to add the ClientReviewFlags table to PostgreSQL database

# Usage: ./add-client-review-flags-table.sh "connection_string"

if [ -z "$1" ]; then
  echo "Error: Connection string is required"
  echo "Usage: ./add-client-review-flags-table.sh \"connection_string\""
  exit 1
fi

CONNECTION_STRING="$1"

echo "Creating ClientReviewFlags table..."

# Extract database name from connection string
DB_NAME=$(echo "$CONNECTION_STRING" | grep -oP "Database=\K[^;]+")
HOST=$(echo "$CONNECTION_STRING" | grep -oP "Host=\K[^;]+")
USER=$(echo "$CONNECTION_STRING" | grep -oP "Username=\K[^;]+")
PORT=$(echo "$CONNECTION_STRING" | grep -oP "Port=\K[^;]+" || echo "5432")

# Create SQL script
SQL_SCRIPT=$(cat << 'EOF'
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
EOF
)

# Execute SQL commands
echo "$SQL_SCRIPT" | PGPASSWORD=$(echo "$CONNECTION_STRING" | grep -oP "Password=\K[^;]+") psql -h "$HOST" -U "$USER" -d "$DB_NAME" -p "$PORT"

if [ $? -eq 0 ]; then
  echo "ClientReviewFlags table created successfully."
else
  echo "Error creating ClientReviewFlags table."
  exit 1
fi
