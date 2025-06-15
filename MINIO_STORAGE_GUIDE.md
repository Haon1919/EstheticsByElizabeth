# 🖼️ Gallery Image Storage with MinIO

This project now supports multiple storage backends for gallery images, including MinIO for local development that mimics cloud blob storage behavior.

## 🚀 Quick Start with MinIO

1. **Start all services including MinIO:**
   ```bash
   cd docker
   ./start-with-minio.sh
   ```

2. **Access the services:**
   - **API:** http://localhost
   - **MinIO Console:** http://localhost:9001 (admin/minioadmin123)
   - **MinIO API:** http://localhost:9000
   - **PostgreSQL:** localhost:5432

3. **Test the setup:**
   ```bash
   cd docker
   ./test-minio-setup.sh
   ```

## 🏗️ Storage Architecture

### Supported Storage Providers

1. **MinIO** (Development) - S3-compatible object storage
2. **Local File System** (Development fallback)
3. **Azure Blob Storage** (Production ready)
4. **AWS S3** (Production ready - requires implementation)

### Configuration

The storage provider is configured via `ImageStorage:Provider` in your settings:

```json
{
  "ImageStorage": {
    "Provider": "MinIO",
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin123",
      "BucketName": "gallery-images",
      "UseSSL": false
    }
  }
}
```

## 🛠️ Development Workflow

1. **Upload images via the admin panel** - Images are stored in MinIO
2. **View images in the gallery** - Served directly from MinIO
3. **Delete images** - Removes from both database and MinIO storage

## 🔄 Switching Storage Providers

Simply change the `ImageStorage:Provider` configuration:

- `MinIO` - Use MinIO container (recommended for development)
- `Local` - Use local file system 
- `Azure` - Use Azure Blob Storage (production)

## 🐳 Docker Services

- **minio** - MinIO object storage server
- **minio-setup** - Automatically creates the gallery-images bucket
- **postgres** - Database
- **api** - Your API with storage abstraction

## 🧪 Testing

The storage service includes a comprehensive interface that makes it easy to:

- Upload images with automatic filename generation
- Delete images safely
- Check if images exist
- Retrieve image streams

## 🔐 Security

- MinIO bucket is configured with public read access for image serving
- All uploads go through validation (file type, size limits)
- Unique filenames prevent conflicts and enhance security

## 🌐 Production Deployment

For production, simply:

1. Set `ImageStorage:Provider` to `Azure`
2. Configure your Azure Blob Storage connection string
3. Deploy as normal - no code changes needed!

The abstraction layer ensures your gallery works identically across all storage providers.
