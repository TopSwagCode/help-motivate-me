# Docker Deployment Guide

This guide explains how to run the entire Help Motivate Me application stack using Docker Compose.

## Prerequisites

- Docker Desktop or Docker Engine + Docker Compose
- At least 4GB of available RAM
- Ports 3000, 5000, 5432, 8025, 4566, and 1025 available

## Quick Start

From the project root directory:

```bash
# Build and start all services
docker compose up --build

# Or run in detached mode (background)
docker compose up -d --build
```

## Services

The docker-compose.yml orchestrates the following services:

| Service | Port | Description |
|---------|------|-------------|
| **frontend** | 5173 | SvelteKit web application |
| **backend** | 5001 | .NET Web API |
| **postgres** | 5432 | PostgreSQL database |
| **mailpit** | 8025 | Email testing (Web UI) |
| **mailpit** | 1025 | SMTP server |
| **localstack** | 4566 | AWS S3 emulator |

## Accessing the Application

- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5001
- **API Swagger**: http://localhost:5001/swagger
- **Mailpit UI**: http://localhost:8025
- **LocalStack**: http://localhost:4566

## Common Commands

```bash
# Start services
docker compose up

# Start in background
docker compose up -d

# Stop services
docker compose down

# Stop and remove volumes (deletes database data!)
docker compose down -v

# View logs
docker compose logs

# View logs for specific service
docker compose logs frontend
docker compose logs backend

# Follow logs in real-time
docker compose logs -f

# Rebuild services after code changes
docker compose up --build

# Rebuild specific service
docker compose up --build frontend

# Restart a service
docker compose restart backend

# Run commands in a service
docker compose exec backend dotnet ef migrations list
docker compose exec postgres psql -U postgres -d helpmotivateme
```

## Database Migrations

The backend automatically applies migrations on startup. If you need to run migrations manually:

```bash
# Check migration status
docker compose exec backend dotnet ef migrations list

# Create a new migration (requires source code mounted)
docker compose exec backend dotnet ef migrations add MigrationName

# Apply migrations
docker compose exec backend dotnet ef database update
```

## Development Workflow

### Making Backend Changes

1. Edit code in `./backend`
2. Rebuild and restart: `docker compose up --build backend`
3. The backend container will automatically recompile

### Making Frontend Changes

1. Edit code in `./frontend`
2. Rebuild and restart: `docker compose up --build frontend`
3. The frontend container will serve the new build

**Note**: For active development with hot-reload, it's recommended to run frontend/backend locally (see GETTING-STARTED.md) and only use Docker for dependencies:

```bash
# Terminal 1: Start only dependencies
cd backend
docker compose up

# Terminal 2: Run backend locally
cd backend/src/HelpMotivateMe.Api
dotnet watch run

# Terminal 3: Run frontend locally
cd frontend
npm run dev
```

## Environment Variables

The root `docker-compose.yml` sets environment variables for all services:

### Backend Environment
- `ASPNETCORE_ENVIRONMENT`: Set to Development
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection
- `Smtp__*`: Mailpit SMTP configuration
- `AWS__*`: LocalStack S3 configuration

### Frontend Environment
- `VITE_API_URL`: Points to backend at http://localhost:5001

To override variables, create a `.env` file in the project root:

```env
# .env
VITE_API_URL=http://localhost:5001
```

## Troubleshooting

### Services Won't Start

```bash
# Check if ports are already in use
lsof -i :5173
lsof -i :5001
lsof -i :5432

# View detailed logs
docker compose logs
```

### Database Connection Issues

```bash
# Check if postgres is healthy
docker compose ps

# Connect to database manually
docker compose exec postgres psql -U postgres -d helpmotivateme
```

### Reset Everything

```bash
# Stop and remove all containers, networks, and volumes
docker compose down -v

# Remove all images
docker compose down --rmi all

# Start fresh
docker compose up --build
```

### Backend Won't Build

Check .NET SDK version in backend Dockerfile matches your project requirements.

### Frontend Build Fails

```bash
# Check Node version
docker compose run frontend node --version

# Clear build cache
docker compose build --no-cache frontend
```

## Production Considerations

This Docker setup is suitable for **development and testing**. For production:

1. **Use production-ready .NET SDK**: Update Dockerfile to use .NET 10 runtime when available
2. **Environment variables**: Use secrets management (Docker secrets, Azure Key Vault, AWS Secrets Manager)
3. **Database**: Use managed PostgreSQL (Azure Database, AWS RDS, etc.)
4. **Reverse proxy**: Add nginx/Traefik for SSL termination and routing
5. **Health checks**: Add proper health check endpoints
6. **Logging**: Configure structured logging with aggregation
7. **Monitoring**: Add application performance monitoring (APM)
8. **Scaling**: Consider Kubernetes for multi-instance deployments

## Network Architecture

All services run on a custom bridge network (`helpmotivateme-network`), allowing them to communicate using service names as hostnames:

- Frontend → Backend: `http://backend:5001`
- Backend → Postgres: `postgres:5432`
- Backend → Mailpit: `mailpit:1025`
- Backend → LocalStack: `http://localstack:4566`

External access is provided through published ports.

## Volumes

Persistent data is stored in named volumes:

- `postgres_data`: PostgreSQL database files
- `localstack_data`: LocalStack S3 storage

These volumes persist between container restarts. Remove them with `docker compose down -v` to reset data.
