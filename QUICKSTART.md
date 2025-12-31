# Quick Start Guide

## 🚀 Run Everything with Docker

```bash
# Start the entire stack
./docker.sh start

# Or manually
docker compose up -d --build
```

**Access:**
- 🌐 Frontend: http://localhost:5173
- 🔌 Backend API: http://localhost:5001
- 📧 Mailpit: http://localhost:8025
- 🗄️ PostgreSQL: localhost:5432

## 📋 Common Commands

### Using Shell Script
```bash
./docker.sh start          # Start all services
./docker.sh stop           # Stop all services
./docker.sh logs           # View all logs
./docker.sh logs-backend   # View backend logs
./docker.sh status         # Check service status
./docker.sh help           # See all commands
```

### Using Makefile
```bash
make start                 # Start all services
make stop                  # Stop all services
make logs                  # View all logs
make logs-backend          # View backend logs
make status                # Check service status
make help                  # See all commands
```

Use whichever you prefer - they do the same thing!

## 🛠️ Development Workflow

### Using Docker (Simple)
```bash
# Start everything
./docker.sh start

# Make code changes in ./backend or ./frontend

# Rebuild after changes
./docker.sh rebuild
```

### Local Development (Hot Reload)
```bash
# Terminal 1: Start dependencies only
cd backend
docker compose up

# Terminal 2: Run backend with hot reload
cd backend/src/HelpMotivateMe.Api
dotnet watch run

# Terminal 3: Run frontend with hot reload
cd frontend
npm install
npm run dev
```

## 🗃️ Database Commands

```bash
./docker.sh db-shell       # Open PostgreSQL shell
./docker.sh db-migrate     # Run migrations
./docker.sh db-reset       # Reset database (careful!)
```

## 🔧 Troubleshooting

```bash
# Ports already in use?
lsof -i :5173
lsof -i :5001

# Services not starting?
./docker.sh logs

# Complete reset (deletes everything!)
./docker.sh reset
```

## 📚 Documentation

- [GETTING-STARTED.md](GETTING-STARTED.md) - Local development setup
- [DOCKER.md](DOCKER.md) - Detailed Docker documentation
- [README.md](README.md) - Full project documentation

## 🏗️ Project Structure

```
help-motivate-me/
├── docker-compose.yml          # Complete stack orchestration
├── docker.sh                   # Helper script
├── backend/
│   ├── Dockerfile             # Backend container
│   ├── docker-compose.yml     # Dependencies only
│   └── src/                   # .NET source code
└── frontend/
    ├── Dockerfile             # Frontend container
    └── src/                   # SvelteKit source code
```

## 🎯 First Time Setup

1. **Install Prerequisites**
   - Docker Desktop (includes Docker Compose)
   - Git

2. **Clone and Start**
   ```bash
   git clone <repo-url>
   cd help-motivate-me
   ./docker.sh start
   ```

3. **Open Application**
   - Go to http://localhost:5173
   - Register a new account
   - Start using the app!

## 💡 Tips

- **Docker vs Local**: Use Docker for quick demos, local development for active coding
- **Logs**: Always check logs if something isn't working: `./docker.sh logs`
- **Clean Start**: If things are weird, try `./docker.sh clean` then `./docker.sh start`
- **Port Conflicts**: Make sure ports 5173, 5001, 5432, 8025 are available

## 🔑 Environment Variables

Copy `.env.example` to `.env` to customize:

```bash
cp .env.example .env
# Edit .env with your values
```

**Optional**: Configure GitHub OAuth for social login (see README.md)
