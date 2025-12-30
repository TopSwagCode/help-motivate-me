# Getting Started with Help Motivate Me

This guide will walk you through setting up and running the Help Motivate Me application locally.

## Prerequisites

Before you begin, ensure you have the following installed:

- **Node.js 18+** - [Download](https://nodejs.org/)
- **.NET 9+ SDK** - [Download](https://dotnet.microsoft.com/download)
- **Docker & Docker Compose** - [Download](https://www.docker.com/products/docker-desktop)

## Quick Start

Follow these steps in order to get the application running:

### 1. Start Development Services

First, start the required services (PostgreSQL, Mailpit, LocalStack) using Docker Compose:

```bash
cd backend
docker compose up -d
```

**Services started:**
- **PostgreSQL** - Database server on `localhost:5432`
  - Database: `helpmotivateme`
  - Username: `postgres`
  - Password: `postgres`
- **Mailpit** - Email testing UI on `http://localhost:8025`
  - SMTP server on `localhost:1025`
- **LocalStack** - S3-compatible storage on `localhost:4566`

**Verify services are running:**
```bash
docker compose ps
```

All services should show status as "Up".

### 2. Start the Backend API

The backend will automatically apply database migrations on startup in development mode.

```bash
cd backend
dotnet run --project src/HelpMotivateMe.Api
```

**Backend API will be available at:** `http://localhost:5000`

**Note:** The first run may take a few moments as it:
- Restores NuGet packages
- Applies database migrations
- Starts the API server

You should see output indicating:
- Database migrations applied
- API listening on `http://localhost:5000`

### 3. Start the Frontend

In a new terminal window:

```bash
cd frontend
npm install  # Only needed on first run or after package changes
npm run dev
```

**Frontend will be available at:** `http://localhost:5173`

The Vite dev server will start and show:
```
VITE v5.x.x  ready in XXXms
➜  Local:   http://localhost:5173/
```

## Accessing the Application

Once all services are running:

1. Open your browser and navigate to **http://localhost:5173**
2. Create a new account or login
3. Start creating goals, habit stacks, and identities!

## Development Tools

### View Sent Emails
- Open **http://localhost:8025** to view emails sent by the application
- Useful for testing email login, password resets, etc.

### API Documentation
- In development mode, OpenAPI documentation is available at `http://localhost:5000/openapi`

### Database Access
Connect to PostgreSQL using your favorite database client:
```
Host: localhost
Port: 5432
Database: helpmotivateme
Username: postgres
Password: postgres
```

## Stopping the Application

### Stop Frontend
Press `Ctrl+C` in the terminal running the frontend

### Stop Backend
Press `Ctrl+C` in the terminal running the backend

### Stop Docker Services
```bash
cd backend
docker compose down
```

To stop and remove all data (databases, etc.):
```bash
docker compose down -v
```

## Troubleshooting

### Port Already in Use
If you get errors about ports being in use:

- **5173** (Frontend): Stop other Vite/dev servers
- **5000** (Backend): Stop other .NET applications
- **5432** (PostgreSQL): Stop other PostgreSQL instances or change the port in `docker-compose.yml`

### Database Connection Issues
1. Ensure Docker services are running: `docker compose ps`
2. Check PostgreSQL logs: `docker compose logs postgres`
3. Try restarting the database: `docker compose restart postgres`

### Migration Issues
If database migrations fail, you can manually apply them:
```bash
cd backend
dotnet ef database update --project src/HelpMotivateMe.Infrastructure --startup-project src/HelpMotivateMe.Api
```

### Frontend Build Errors
1. Delete `node_modules` and reinstall:
   ```bash
   cd frontend
   rm -rf node_modules package-lock.json
   npm install
   ```

### Backend Build Errors
1. Clean and rebuild:
   ```bash
   cd backend
   dotnet clean
   dotnet build
   ```

## Next Steps

- Check out [README.md](README.md) for detailed feature documentation
- Review [ARCHITECTURE.md](ARCHITECTURE.md) for technical architecture details
- Start coding! The dev servers support hot reload for both frontend and backend

## Optional: GitHub OAuth Setup

To enable GitHub OAuth login:

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Create a new OAuth App:
   - **Application name:** Help Motivate Me (Local)
   - **Homepage URL:** `http://localhost:5173`
   - **Authorization callback URL:** `http://localhost:5000/signin-github`
3. Copy the Client ID and Client Secret
4. Update `backend/src/HelpMotivateMe.Api/appsettings.Development.json`:
   ```json
   {
     "OAuth": {
       "GitHub": {
         "ClientId": "your-client-id",
         "ClientSecret": "your-client-secret"
       }
     }
   }
   ```
5. Restart the backend

## Need Help?

- Check existing issues on GitHub
- Review the architecture documentation
- Ensure all prerequisites are installed correctly
- Verify all services are running with `docker compose ps`

Happy coding! 🚀
