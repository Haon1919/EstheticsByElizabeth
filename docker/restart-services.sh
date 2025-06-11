#!/bin/bash

# Restart Services Script
# Stops and starts Docker services by calling existing scripts

set -e  # Exit on any error

echo "🔄 Restarting EstheticsByElizabeth services..."

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "📍 Script directory: $SCRIPT_DIR"

# Stop services using existing script
echo "🛑 Stopping services..."
"$SCRIPT_DIR/stop-services.sh"

# Optional: Remove volumes to completely reset database (uncomment if needed)
# echo "🗑️  Removing volumes for fresh database..."
# docker-compose -f "$SCRIPT_DIR/docker-compose.yml" down -v

# Start services using existing script
echo "🚀 Starting services..."
"$SCRIPT_DIR/start-services.sh"

echo "🎉 Services restart complete!"
