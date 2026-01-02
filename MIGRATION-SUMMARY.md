# Migration Summary: S3 to Local File Storage

## Changes Made

### ✅ New Files Created

1. **LocalFileStorageService.cs** - New local file storage implementation
   - Path: `backend/src/HelpMotivateMe.Infrastructure/Services/LocalFileStorageService.cs`
   - Implements `IStorageService` interface
   - Stores files in local filesystem with security protections
   - Auto-creates directories as needed

2. **FilesController.cs** - New file serving endpoint
   - Path: `backend/src/HelpMotivateMe.Api/Controllers/FilesController.cs`
   - Serves files from `/api/files/{*filepath}`
   - Includes path traversal protection
   - Automatic content-type detection

3. **LOCAL-STORAGE.md** - Comprehensive documentation
   - Complete guide for local storage setup
   - Migration guide back to S3 if needed
   - Security and best practices

### ✅ Files Modified

1. **Program.cs** - Updated dependency injection
   - Removed: AWS S3 client configuration
   - Added: LocalFileStorageService registration
   - Removed: `using Amazon.S3;`

2. **appsettings.json** - Updated configuration
   - Removed: S3 configuration section
   - Added: LocalStorage configuration (BasePath, BaseUrl)

3. **appsettings.Development.json** - Updated development configuration
   - Removed: S3 LocalStack configuration
   - Added: LocalStorage configuration for development

4. **HelpMotivateMe.Infrastructure.csproj** - Removed AWS dependency
   - Removed: `AWSSDK.S3` NuGet package reference

5. **docker-compose.yml (root)** - Updated Docker services
   - Removed: LocalStack service
   - Removed: localstack_data volume
   - Added: uploads_data volume for backend
   - Updated: Backend environment variables
   - Removed: Backend dependency on LocalStack

6. **docker-compose.yml (backend)** - Updated backend Docker
   - Removed: LocalStack service
   - Removed: localstack_data volume

7. **.gitignore** - Added local storage directories
   - Added: `backend/uploads/` and `uploads/` to gitignore

### ✅ Files Deleted

1. **S3StorageService.cs** - Old S3 implementation (no longer needed)
2. **init-localstack.sh** - LocalStack initialization script (no longer needed)

## Configuration Changes

### Environment Variables

**Before (S3):**
```yaml
- AWS__ServiceURL=http://localstack:4566
- AWS__Region=us-east-1
- AWS__AccessKey=test
- AWS__SecretKey=test
- AWS__S3BucketName=helpmotivateme-uploads
```

**After (Local Storage):**
```yaml
- LocalStorage__BasePath=/app/uploads
- LocalStorage__BaseUrl=http://localhost:5001
```

### Docker Volumes

**Before:**
- `postgres_data` - Database
- `localstack_data` - S3 storage

**After:**
- `postgres_data` - Database
- `uploads_data` - Local file storage

## Testing Checklist

Before deploying to production, test the following:

- [ ] Upload a file through the journal/image endpoints
- [ ] Verify file is saved in the uploads directory
- [ ] Access the file via `/api/files/{filepath}` endpoint
- [ ] Delete a file and verify it's removed from filesystem
- [ ] Test with multiple file types (images, documents)
- [ ] Verify files persist across container restarts (Docker volume)
- [ ] Test path traversal protection (attempt `../` in paths)

## Local Development Setup

1. **Clone and setup:**
   ```bash
   cd backend
   dotnet restore
   ```

2. **Run locally (without Docker):**
   ```bash
   cd backend/src/HelpMotivateMe.Api
   dotnet run
   ```
   Files will be stored in `./uploads` directory.

3. **Run with Docker:**
   ```bash
   docker-compose up --build
   ```
   Files will be stored in the `uploads_data` Docker volume.

## Future Migration Path

The architecture is designed for easy migration to cloud storage:

### To Switch to S3:
1. Create new `S3StorageService` implementing `IStorageService`
2. Add `AWSSDK.S3` NuGet package
3. Update `Program.cs` to register S3 service
4. Update configuration with S3 credentials
5. No changes needed in controllers or business logic

### To Switch to Azure Blob Storage:
1. Create `AzureBlobStorageService` implementing `IStorageService`
2. Add `Azure.Storage.Blobs` NuGet package
3. Update `Program.cs` registration
4. Update configuration
5. No changes needed in controllers or business logic

## Benefits of This Migration

✅ **Simplified Development** - No LocalStack container needed  
✅ **Reduced Complexity** - Fewer moving parts in Docker setup  
✅ **Lower Overhead** - No AWS SDK dependencies  
✅ **Easier Debugging** - Direct filesystem access  
✅ **Future-Proof** - Clean interface allows easy migration later  
✅ **Cost-Effective** - No cloud storage costs during development  

## Rollback Plan

If you need to rollback to S3:

1. Restore deleted files:
   - `backend/src/HelpMotivateMe.Infrastructure/Services/S3StorageService.cs`
   - `backend/init-localstack.sh`

2. Restore changes to:
   - `backend/src/HelpMotivateMe.Api/Program.cs`
   - `backend/src/HelpMotivateMe.Infrastructure/HelpMotivateMe.Infrastructure.csproj`
   - `backend/src/HelpMotivateMe.Api/appsettings.json`
   - `docker-compose.yml`

3. Add back NuGet package:
   ```bash
   dotnet add package AWSSDK.S3
   ```

## Questions or Issues?

Refer to `LOCAL-STORAGE.md` for detailed documentation on:
- Architecture and design decisions
- Configuration options
- Security considerations
- Performance optimization
- Troubleshooting guide
