#!/bin/bash
echo "Starting simple test server to check image paths..."
echo "This will help diagnose image loading issues without requiring Angular CLI"

# Install http-server if needed (simpler than using our custom script)
if ! command -v npx &> /dev/null; then
  echo "Installing npx to run the server..."
  npm install -g npx
fi

# Create symlinks to ensure images are accessible from multiple paths
mkdir -p public/assets/images
mkdir -p public/images

# Copy images to multiple locations for maximum compatibility testing
if [ -d "assets/images" ]; then
  echo "Copying images from assets/images to multiple test locations..."
  cp -r assets/images/* public/assets/images/ 2>/dev/null || echo "No images in assets/images"
  cp -r assets/images/* public/images/ 2>/dev/null || echo "No images in assets/images"
fi

if [ -d "src/assets/images" ]; then
  echo "Copying images from src/assets/images to multiple test locations..."
  cp -r src/assets/images/* public/assets/images/ 2>/dev/null || echo "No images in src/assets/images"
  cp -r src/assets/images/* public/images/ 2>/dev/null || echo "No images in src/assets/images"
fi

echo ""
echo "Images in public/assets/images:"
ls -la public/assets/images/ 2>/dev/null || echo "No images found"

echo ""
echo "Starting server..."
echo "Test page will be available at: http://localhost:8080/test-image.html"
echo "Press Ctrl+C to stop the server"
echo ""

# Run the http-server
npx http-server -p 8080 --cors 