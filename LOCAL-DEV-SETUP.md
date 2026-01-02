# Local Development Setup (Mac M4)

## Quick Start

### Running Locally (without Docker)

1. **Navigate to the backend API directory:**
   ```bash
   cd backend/src/HelpMotivateMe.Api
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Files will be stored in:**
   ```
   backend/src/HelpMotivateMe.Api/uploads/
   ```

The `uploads` directory will be created automatically on first upload.

### Configuration for Local Development

The `appsettings.Development.json` is configured for local Mac development:

```json
{
  "LocalStorage": {
    "BasePath": "uploads",
    "BaseUrl": "http://localhost:5001"
  }
}
```

- `BasePath: "uploads"` - Uses a relative path (creates folder in the API project directory)
- This avoids the `/app` read-only filesystem issue on Mac

### Running with Docker

If you want to run the full stack with Docker:

```bash
docker-compose up --build
```

The docker-compose.yml already configures:
- Volume mount: `uploads_data:/app/uploads`
- Environment override: `LocalStorage__BasePath=/app/uploads`

Docker can write to `/app/uploads` because it's inside the container with proper permissions.

## Troubleshooting

### Error: "Read-only file system : '/app'"

**Solution:** You're running locally (not in Docker) but the config points to `/app/uploads`.

Fix: The `appsettings.Development.json` should use `"BasePath": "uploads"` (relative path).

### Uploads Directory Not Created

The directory is created automatically on first upload. To create it manually:

```bash
cd backend/src/HelpMotivateMe.Api
mkdir uploads
```

### Permission Denied

Ensure your user has write permissions:

```bash
cd backend/src/HelpMotivateMe.Api
chmod 755 uploads
```

## Environment-Specific Paths

### Local Mac Development (dotnet run)
- **Config:** `appsettings.Development.json`
- **Path:** `"uploads"` (relative)
- **Physical location:** `backend/src/HelpMotivateMe.Api/uploads/`

### Docker (docker-compose)
- **Config:** Environment variable override in `docker-compose.yml`
- **Path:** `/app/uploads` (absolute, inside container)
- **Physical location:** Docker volume `uploads_data`

### Production
- **Config:** Environment variables or `appsettings.Production.json`
- **Path:** Absolute path like `/var/app/uploads` or switch to cloud storage

## Switching Between Environments

The app automatically uses the right configuration:

- Running `dotnet run` → Uses `appsettings.Development.json` → `uploads/` folder
- Running `docker-compose up` → Uses env vars → `/app/uploads` in container
- Running in production → Uses production config

No code changes needed! 🎉

## File Access

Upload a file through your API, then access it at:

```
http://localhost:5001/api/files/{filepath}
```

Example:
```
http://localhost:5001/api/files/journals/user123/photo.jpg
```

## Clean Up Test Files

```bash
cd backend/src/HelpMotivateMe.Api
rm -rf uploads/*
```

Or in Docker:
```bash
docker-compose down -v  # Removes volumes including uploads
```
