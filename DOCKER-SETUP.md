# Docker Setup Summary

## Files Created

### Root Level
1. **docker-compose.yml** - Main orchestration file
   - Starts all services: frontend, backend, postgres, mailpit, localstack
   - Configures network and volumes
   - Sets environment variables
   - Manages dependencies and health checks

2. **docker.sh** - Helper script for Docker management
   - Quick commands: start, stop, restart, rebuild
   - Log viewing: logs, logs-frontend, logs-backend
   - Database tools: db-shell, db-migrate, db-reset
   - Cleanup: clean, reset

3. **.env.example** - Environment variable template
   - Documents all configurable variables
   - Safe to commit (no secrets)
   - Copy to .env for customization

4. **DOCKER.md** - Comprehensive Docker documentation
   - Architecture explanation
   - Service descriptions
   - Commands reference
   - Troubleshooting guide
   - Production considerations

5. **QUICKSTART.md** - Quick reference guide
   - Fast getting started
   - Common commands
   - Development workflows
   - First-time setup

### Backend
6. **backend/Dockerfile** - Backend container definition
   - Multi-stage build (build → publish → runtime)
   - .NET 9 SDK and runtime
   - Exposes port 5000

7. **backend/.dockerignore** - Build optimization
   - Excludes bin/, obj/, node_modules/
   - Reduces build context size

### Frontend
8. **frontend/Dockerfile** - Frontend container definition
   - Multi-stage build (build → runtime)
   - Node 20 Alpine
   - Builds static site with Vite
   - Serves with 'serve' on port 3000

9. **frontend/.dockerignore** - Build optimization
   - Excludes node_modules/, build/
   - Reduces build context size

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Docker Network                      │
│             (helpmotivateme-network)                 │
│                                                      │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐     │
│  │ Frontend │───▶│ Backend  │───▶│ Postgres │     │
│  │ (int:3000)   │ (int:5001)   │  :5432   │     │
│  └──────────┘    └──────────┘    └──────────┘     │
│                        │               │            │
│                        │               │            │
│                   ┌────▼────┐    ┌────▼─────┐     │
│                   │ Mailpit │    │LocalStack│     │
│                   │:8025/1025    │  :4566   │     │
│                   └─────────┘    └──────────┘     │
│                                                      │
└─────────────────────────────────────────────────────┘
          │         │         │         │        │
          ▼         ▼         ▼         ▼        ▼
       :5173     :5001     :5432     :8025    :4566
       (Host)    (Host)    (Host)    (Host)   (Host)
```

## Service Overview

| Service | Internal | External | Purpose |
|---------|----------|----------|---------|
| Frontend | frontend:3000 | localhost:5173 | SvelteKit UI |
| Backend | backend:5001 | localhost:5001 | .NET API |
| Postgres | postgres:5432 | localhost:5432 | Database |
| Mailpit | mailpit:1025/8025 | localhost:8025 | Email Testing |
| LocalStack | localstack:4566 | localhost:4566 | S3 Emulator |

## Usage

### Quick Start
```bash
# Start everything
./docker.sh start

# View logs
./docker.sh logs

# Stop everything
./docker.sh stop
```

### Development Workflow

**Option 1: Full Docker**
- Good for: Demos, testing, production-like environment
- Hot reload: No (need to rebuild)
- Startup: Slower (initial build)
- Command: `./docker.sh start`

**Option 2: Hybrid (Dependencies in Docker, Apps Local)**
- Good for: Active development
- Hot reload: Yes
- Startup: Fast
- Commands:
  ```bash
  cd backend && docker compose up          # Dependencies
  cd backend/src/HelpMotivateMe.Api && dotnet watch run  # Backend
  cd frontend && npm run dev               # Frontend
  ```

### Database Management
```bash
# Connect to database
./docker.sh db-shell

# Run migrations
./docker.sh db-migrate

# Reset database (destructive!)
./docker.sh db-reset
```

### Cleaning Up
```bash
# Stop and remove volumes (deletes data)
./docker.sh clean

# Complete reset (removes images too)
./docker.sh reset
```

## Environment Variables

The root `.env` file (copied from `.env.example`) overrides defaults:

```bash
# Copy example
cp .env.example .env

# Edit with your values
nano .env
```

Common overrides:
- `VITE_API_URL` - Frontend API URL
- `POSTGRES_PASSWORD` - Database password
- `GITHUB_CLIENT_ID` - OAuth client ID
- `GITHUB_CLIENT_SECRET` - OAuth secret

## Volumes

Persistent data stored in Docker volumes:

- **postgres_data** - Database files
  - Persists between restarts
  - Removed with `docker compose down -v`

- **localstack_data** - S3 storage
  - Persists uploaded files
  - Removed with `docker compose down -v`

## Network

All services on `helpmotivateme-network` (bridge):
- Services communicate via service names (e.g., `http://backend:5001`)
- External access via published ports
- Isolated from other Docker networks

## Next Steps

1. **Review Documentation**
   - Read DOCKER.md for detailed info
   - Check QUICKSTART.md for quick reference

2. **Customize Environment**
   - Copy .env.example to .env
   - Add GitHub OAuth credentials (optional)

3. **Start Developing**
   - Use `./docker.sh start` for quick demo
   - Use hybrid approach for development

4. **Monitor & Debug**
   - Use `./docker.sh logs` to view output
   - Use `./docker.sh status` to check health
   - Use `./docker.sh db-shell` to inspect database

## Troubleshooting

**Build Fails**
```bash
# Check Docker is running
docker info

# View detailed logs
./docker.sh logs

# Clean rebuild
./docker.sh clean
./docker.sh start
```

**Port Conflicts**
```bash
# Find process using port
lsof -i :3000
lsof -i :5000

# Kill process or change ports in docker-compose.yml
```

**Database Issues**
```bash
# Check postgres health
docker compose ps

# View postgres logs
./docker.sh logs-db

# Reset database
./docker.sh db-reset
```

## Production Notes

This setup is **development-focused**. For production:

1. Use managed PostgreSQL (not Docker)
2. Use environment variable secrets management
3. Add nginx reverse proxy with SSL
4. Configure proper health checks
5. Set up monitoring and logging
6. Use container orchestration (Kubernetes)
7. Implement proper backup strategy
8. Use production-ready .NET runtime image

See DOCKER.md for detailed production considerations.
