#!/usr/bin/env python3
"""
Upload images to MinIO storage to match database gallery entries.
This script maps available source images to the gallery object keys defined in the database.
"""

import os
import time
import shutil
from pathlib import Path
from minio import Minio
from minio.error import S3Error
import sys

# MinIO configuration
MINIO_ENDPOINT = "minio:9000"  # Use service name for container-to-container communication
MINIO_ACCESS_KEY = "minioadmin"
MINIO_SECRET_KEY = "minioadmin123"
BUCKET_NAME = "gallery-images"

# Source images mapping - map available images to gallery object keys
SOURCE_IMAGE_MAP = {
    # Available source images from frontend
    'image2.jpeg': [
        'images/gallery/facial-treatment-1.jpg',
        'images/gallery/before-after-facial.jpg',
        'images/gallery/eyebrow-waxing.jpg',
        'images/gallery/body-treatment.jpg',
        'images/gallery/premium-skincare.jpg'
    ],
    'image3.jpeg': [
        'images/gallery/facial-treatment-2.jpg',
        'images/gallery/skincare-transformation.jpg',
        'images/gallery/professional-waxing.jpg',
        'images/gallery/body-care-services.jpg',
        'images/gallery/product-education.jpg'
    ],
    'image7.jpeg': [
        'images/gallery/dermaplaning-treatment.jpg',
        'images/gallery/treatment-room.jpg',
        'images/gallery/makeup-services.jpg'
    ],
    'cat.jpg': [
        'images/gallery/studio-atmosphere.jpg'  # Cat as studio mascot!
    ],
    'cat.jpeg': [
        'images/gallery/bridal-makeup.jpg'
    ]
}

def wait_for_minio(max_retries=30, retry_delay=2):
    """Wait for MinIO to be available."""
    print("Waiting for MinIO to be available...")
    
    for attempt in range(max_retries):
        try:
            client = Minio(
                MINIO_ENDPOINT,
                access_key=MINIO_ACCESS_KEY,
                secret_key=MINIO_SECRET_KEY,
                secure=False
            )
            # Try to list buckets to test connection
            list(client.list_buckets())
            print(f"✅ MinIO is available after {attempt + 1} attempts")
            return client
        except Exception as e:
            if attempt < max_retries - 1:
                print(f"⏳ MinIO not ready yet (attempt {attempt + 1}/{max_retries}): {e}")
                time.sleep(retry_delay)
            else:
                print(f"❌ Failed to connect to MinIO after {max_retries} attempts: {e}")
                return None
    
    return None

def ensure_bucket_exists(client):
    """Ensure the gallery-images bucket exists."""
    try:
        if not client.bucket_exists(BUCKET_NAME):
            client.make_bucket(BUCKET_NAME)
            print(f"✅ Created bucket: {BUCKET_NAME}")
        else:
            print(f"✅ Bucket already exists: {BUCKET_NAME}")
        return True
    except S3Error as e:
        print(f"❌ Failed to create/check bucket: {e}")
        return False

def find_source_image(image_name):
    """Find the source image file in various possible locations."""
    base_path = Path("/app/source-images")  # Docker container path
    possible_paths = [
        base_path / image_name,
        base_path / f"frontend/public/assets/images/{image_name}",
        base_path / f"frontend/public/images/{image_name}",
        base_path / f"frontend/assets/images/{image_name}",
        base_path / f"frontend/src/assets/images/{image_name}"
    ]
    
    for path in possible_paths:
        if path.exists():
            return path
    
    print(f"⚠️  Source image not found: {image_name}")
    return None

def upload_images(client):
    """Upload images to MinIO based on the source mapping."""
    uploaded_count = 0
    failed_count = 0
    
    print(f"\n🚀 Starting image upload process...")
    
    for source_image, object_keys in SOURCE_IMAGE_MAP.items():
        source_path = find_source_image(source_image)
        
        if not source_path:
            print(f"❌ Skipping {source_image} - file not found")
            failed_count += len(object_keys)
            continue
            
        print(f"\n📁 Processing source image: {source_image}")
        
        for object_key in object_keys:
            try:
                # Upload the image
                client.fput_object(
                    BUCKET_NAME,
                    object_key,
                    str(source_path),
                    content_type="image/jpeg"
                )
                print(f"  ✅ Uploaded: {object_key}")
                uploaded_count += 1
                
            except S3Error as e:
                print(f"  ❌ Failed to upload {object_key}: {e}")
                failed_count += 1
    
    print(f"\n📊 Upload Summary:")
    print(f"  ✅ Successfully uploaded: {uploaded_count} images")
    print(f"  ❌ Failed uploads: {failed_count} images")
    
    return uploaded_count > 0

def main():
    """Main function to upload all gallery images."""
    print("🎨 Gallery Image Upload Script")
    print("=" * 50)
    
    # Wait for MinIO to be available
    client = wait_for_minio()
    if not client:
        print("❌ Could not connect to MinIO. Exiting.")
        sys.exit(1)
    
    # Ensure bucket exists
    if not ensure_bucket_exists(client):
        print("❌ Could not create/access bucket. Exiting.")
        sys.exit(1)
    
    # Upload images
    success = upload_images(client)
    
    if success:
        print("\n🎉 Image upload completed successfully!")
        print("🔗 Gallery images are now available in MinIO storage")
    else:
        print("\n⚠️  Image upload completed with errors")
        print("📋 Check the logs above for details")
    
    # List uploaded objects for verification
    try:
        print(f"\n📋 Objects in bucket '{BUCKET_NAME}':")
        objects = client.list_objects(BUCKET_NAME, recursive=True)
        for obj in objects:
            print(f"  📄 {obj.object_name}")
    except Exception as e:
        print(f"❌ Could not list bucket contents: {e}")

if __name__ == "__main__":
    main()
