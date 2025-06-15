#!/usr/bin/env python3
"""
Single image upload to MinIO storage - called from C# API
This script uploads a single image file to MinIO and returns the result.
"""

import os
import sys
import time
from pathlib import Path
from minio import Minio
from minio.error import S3Error
import json

# MinIO configuration
MINIO_ENDPOINT = "minio:9000"  # Use service name for container-to-container communication
MINIO_ACCESS_KEY = "minioadmin"
MINIO_SECRET_KEY = "minioadmin123"
BUCKET_NAME = "gallery-images"

def wait_for_minio(max_retries=10, retry_delay=1):
    """Wait for MinIO to be available."""
    print("Waiting for MinIO to be available...", file=sys.stderr)
    
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
            print(f"✅ MinIO is available after {attempt + 1} attempts", file=sys.stderr)
            return client
        except Exception as e:
            if attempt < max_retries - 1:
                print(f"⏳ MinIO not ready yet (attempt {attempt + 1}/{max_retries}): {e}", file=sys.stderr)
                time.sleep(retry_delay)
            else:
                print(f"❌ Failed to connect to MinIO after {max_retries} attempts: {e}", file=sys.stderr)
                return None
    
    return None

def ensure_bucket_exists(client):
    """Ensure the gallery-images bucket exists."""
    try:
        if not client.bucket_exists(BUCKET_NAME):
            client.make_bucket(BUCKET_NAME)
            print(f"✅ Created bucket: {BUCKET_NAME}", file=sys.stderr)
        else:
            print(f"✅ Bucket already exists: {BUCKET_NAME}", file=sys.stderr)
        return True
    except S3Error as e:
        print(f"❌ Failed to create/check bucket: {e}", file=sys.stderr)
        return False

def upload_single_image(client, file_path, object_key, content_type="image/jpeg"):
    """Upload a single image to MinIO."""
    try:
        print(f"📤 Uploading: {file_path} -> {object_key}", file=sys.stderr)
        
        # Check if source file exists
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"Source file not found: {file_path}")
        
        # Get file size for logging
        file_size = os.path.getsize(file_path)
        print(f"📊 File size: {file_size} bytes", file=sys.stderr)
        
        # Upload the image using fput_object (same as working Python script)
        client.fput_object(
            BUCKET_NAME,
            object_key,
            file_path,
            content_type=content_type
        )
        
        print(f"✅ Upload successful: {object_key}", file=sys.stderr)
        
        # Verify upload by checking object info
        try:
            stat_result = client.stat_object(BUCKET_NAME, object_key)
            print(f"✅ Verification: Size={stat_result.size}, ETag={stat_result.etag}", file=sys.stderr)
            
            return {
                "success": True,
                "object_key": object_key,
                "size": stat_result.size,
                "etag": stat_result.etag,
                "message": "Upload successful"
            }
        except Exception as verify_error:
            print(f"⚠️ Upload completed but verification failed: {verify_error}", file=sys.stderr)
            return {
                "success": True,
                "object_key": object_key,
                "size": file_size,
                "etag": None,
                "message": "Upload completed but verification failed"
            }
        
    except Exception as e:
        print(f"❌ Upload failed: {e}", file=sys.stderr)
        return {
            "success": False,
            "object_key": object_key,
            "error": str(e),
            "message": "Upload failed"
        }

def main():
    """Main function to upload a single image."""
    if len(sys.argv) != 3:
        print("Usage: python upload-single-image.py <file_path> <object_key>", file=sys.stderr)
        result = {
            "success": False,
            "error": "Invalid arguments",
            "message": "Usage: python upload-single-image.py <file_path> <object_key>"
        }
        print(json.dumps(result))
        sys.exit(1)
    
    file_path = sys.argv[1]
    object_key = sys.argv[2]
    
    print(f"🐍 Python Single Image Upload", file=sys.stderr)
    print(f"📁 Source: {file_path}", file=sys.stderr)
    print(f"🎯 Target: {object_key}", file=sys.stderr)
    
    # Connect to MinIO
    client = wait_for_minio()
    if not client:
        result = {
            "success": False,
            "error": "Could not connect to MinIO",
            "message": "MinIO connection failed"
        }
        print(json.dumps(result))
        sys.exit(1)
    
    # Ensure bucket exists
    if not ensure_bucket_exists(client):
        result = {
            "success": False,
            "error": "Could not create/access bucket",
            "message": "Bucket access failed"
        }
        print(json.dumps(result))
        sys.exit(1)
    
    # Upload the image
    result = upload_single_image(client, file_path, object_key)
    
    # Output result as JSON for C# to parse
    print(json.dumps(result))
    
    if result["success"]:
        sys.exit(0)
    else:
        sys.exit(1)

if __name__ == "__main__":
    main()
