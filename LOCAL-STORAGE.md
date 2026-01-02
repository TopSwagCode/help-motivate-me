# Local File Storage Migration

## Overview

The application has been migrated from AWS S3/LocalStack storage to local file storage. This simplifies development and deployment while maintaining a clean, extensible storage interface.

## Architecture

### Storage Interface

All storage operations use the `IStorageService` interface defined in `HelpMotivateMe.Core/Interfaces/IStorageService.cs`:

```csharp
public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
    string GetPresignedUrl(string key);
}
```

This interface is storage-agnostic, allowing easy switching between different storage implementations.

### Current Implementation

**LocalFileStorageService** (`HelpMotivateMe.Infrastructure/Services/LocalFileStorageService.cs`):
- Stores files in a local directory on the server filesystem
- Generates URLs pointing to the file serving endpoint
- Includes path traversal protection
- Creates directories automatically as needed

## Configuration

### appsettings.json

```json
{
  "LocalStorage": {
    "BasePath": "/app/uploads",
    "BaseUrl": "http://localhost:5001"
  }
}
```

- **BasePath**: Directory where files are stored (absolute or relative path)
- **BaseUrl**: Base URL for generating file URLs (used by `GetPresignedUrl`)

### Docker Configuration

The `docker-compose.yml` includes a volume mount for persistent storage:

```yaml
backend:
  volumes:
    - uploads_data:/app/uploads
  environment:
    - LocalStorage__BasePath=/app/uploads
    - LocalStorage__BaseUrl=http://localhost:5001
```

### Local Development

For local development (without Docker), you can configure:

```json
{
  "LocalStorage": {
    "BasePath": "./uploads",
    "BaseUrl": "http://localhost:5001"
  }
}
```

The directory will be created automatically if it doesn't exist.

## File Serving

Files are served through the `FilesController` at `/api/files/{*filepath}`:

- **Security**: Path traversal protection prevents accessing files outside the storage directory
- **Content-Type**: Automatically determined based on file extension
- **Range Requests**: Supports partial content requests for media streaming

Example URL: `http://localhost:5001/api/files/journals/2025/01/photo.jpg`

## Usage in Code

The storage service is injected via dependency injection:

```csharp
public class JournalController : ControllerBase
{
    private readonly IStorageService _storage;

    public JournalController(IStorageService storage)
    {
        _storage = storage;
    }

    // Upload a file
    var key = $"journals/{userId}/{Guid.NewGuid()}.jpg";
    await _storage.UploadAsync(fileStream, key, "image/jpeg");

    // Get URL to access file
    var url = _storage.GetPresignedUrl(key);

    // Delete file
    await _storage.DeleteAsync(key);
}
```

## Migration Back to S3 (Future)

If you need to migrate back to S3 or another cloud storage:

### 1. Create S3 Implementation

Create a new `S3StorageService` class implementing `IStorageService`:

```csharp
public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    
    // Implement interface methods using S3 SDK
}
```

### 2. Update Dependencies

Add AWS SDK NuGet package:

```bash
dotnet add package AWSSDK.S3
```

### 3. Update Configuration

In `Program.cs`:

```csharp
// Replace LocalFileStorageService with S3StorageService
builder.Services.AddSingleton<IAmazonS3>(/* S3 client configuration */);
builder.Services.AddSingleton<IStorageService, S3StorageService>();
```

In `appsettings.json`:

```json
{
  "S3": {
    "BucketName": "your-bucket-name",
    "Region": "us-east-1"
  }
}
```

### 4. No Code Changes Needed

Because controllers use the `IStorageService` interface, no changes are needed in controllers or other application code. The implementation is swapped transparently.

## Alternative Storage Implementations

The same pattern can be used for other storage providers:

- **Azure Blob Storage**: Implement `IStorageService` using Azure.Storage.Blobs
- **MinIO**: Implement using MinIO SDK (S3-compatible)
- **Google Cloud Storage**: Implement using Google.Cloud.Storage.V1
- **Hybrid**: Use different implementations based on environment or configuration

## File Organization Best Practices

Organize files with logical keys:

```
journals/{userId}/{entryId}/{filename}
avatars/{userId}/{filename}
goal-images/{userId}/{goalId}/{filename}
```

This structure:
- Prevents name collisions
- Makes cleanup easier (delete all files for a user/resource)
- Improves security (validate userId ownership)

## Security Considerations

1. **Path Traversal**: The `LocalFileStorageService` normalizes paths to prevent `../` attacks
2. **File Serving**: The `FilesController` validates paths stay within the base directory
3. **Authentication**: Add `[Authorize]` attribute to `FilesController` if files should be private
4. **File Types**: Consider validating file types/extensions on upload
5. **Size Limits**: Configure max file size in controllers or middleware

## Maintenance

### Backup

Ensure the uploads volume is included in your backup strategy:

```bash
# Backup Docker volume
docker run --rm -v uploads_data:/data -v $(pwd):/backup \
  alpine tar czf /backup/uploads-backup.tar.gz /data
```

### Cleanup

Implement cleanup jobs for orphaned files (files without corresponding database records):

```csharp
// Example: Delete files for deleted journal entries
var orphanedKeys = /* query database for deleted entries */;
await _storage.DeleteManyAsync(orphanedKeys);
```

## Performance

For production at scale, consider:

- **CDN**: Serve files through a CDN for better performance
- **Cloud Storage**: Migrate to S3/Azure Blob for scalability and durability
- **Caching**: Add cache headers in `FilesController`
- **Compression**: Add response compression middleware

## Troubleshooting

### Files Not Accessible

- Check `LocalStorage:BasePath` configuration
- Verify directory permissions (container must have write access)
- Check volume mount in `docker-compose.yml`

### Container Crashes on Startup

- Ensure the base path directory can be created
- Check for filesystem permission issues

### Files Disappear on Restart

- Verify the volume is properly mounted in Docker
- Ensure you're using a named volume, not a bind mount without persistence
