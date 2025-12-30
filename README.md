# Help Motivate Me

A modern TODO/task/motivation web application built with SvelteKit and .NET Core 10.

## Tech Stack

- **Frontend**: SvelteKit (static SPA) + Tailwind CSS + TypeScript
- **Backend**: .NET Core 10 Web API
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: Cookie-based with GitHub OAuth support

## Prerequisites

- Node.js 18+
- .NET 10 SDK
- Docker (for PostgreSQL)

## Getting Started

### 1. Start PostgreSQL

```bash
cd backend
docker compose up -d
```

This starts PostgreSQL on `localhost:5432` with:
- Database: `helpmotivateme`
- Username: `postgres`
- Password: `postgres`

### 2. Run Database Migrations

```bash
cd backend
dotnet ef migrations add InitialCreate --project src/HelpMotivateMe.Infrastructure --startup-project src/HelpMotivateMe.Api
dotnet ef database update --project src/HelpMotivateMe.Infrastructure --startup-project src/HelpMotivateMe.Api
```

Or let the API auto-migrate on startup (development mode only).

### 3. Configure GitHub OAuth (Optional)

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Create a new OAuth App:
   - Application name: Help Motivate Me (Local)
   - Homepage URL: `http://localhost:5173`
   - Authorization callback URL: `http://localhost:5000/api/auth/callback/github`
3. Copy Client ID and Client Secret
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

### 4. Start the Backend

```bash
cd backend
dotnet run --project src/HelpMotivateMe.Api
```

The API will be available at `http://localhost:5000`

### 5. Start the Frontend

```bash
cd frontend
npm install
npm run dev
```

The app will be available at `http://localhost:5173`

## Project Structure

```
help-motivate-me/
├── backend/
│   ├── docker-compose.yml          # PostgreSQL container
│   ├── HelpMotivateMe.sln
│   └── src/
│       ├── HelpMotivateMe.Api/      # Web API project
│       ├── HelpMotivateMe.Core/     # Entities, DTOs, Enums
│       └── HelpMotivateMe.Infrastructure/  # EF Core, Data
└── frontend/
    ├── src/
    │   ├── lib/
    │   │   ├── api/                 # API client functions
    │   │   ├── stores/              # Svelte stores
    │   │   └── types/               # TypeScript types
    │   └── routes/                  # SvelteKit pages
    ├── svelte.config.js
    └── tailwind.config.js
```

## Features

- **Authentication**: Username/password + GitHub OAuth
- **Goals**: Create, edit, delete, and complete goals
- **Tasks**: Add tasks to goals with subtask support
- **Categories**: Organize goals with tags/categories
- **Repeatable Tasks**: Daily, weekly, monthly recurring tasks
- **Modern UI**: Clean, minimalistic design with smooth animations

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login with credentials
- `POST /api/auth/logout` - Logout
- `GET /api/auth/me` - Get current user
- `GET /api/auth/external/github` - Initiate GitHub OAuth

### Goals
- `GET /api/goals` - List goals
- `POST /api/goals` - Create goal
- `GET /api/goals/{id}` - Get goal
- `PUT /api/goals/{id}` - Update goal
- `DELETE /api/goals/{id}` - Delete goal
- `PATCH /api/goals/{id}/complete` - Toggle complete

### Tasks
- `GET /api/goals/{goalId}/tasks` - List tasks
- `POST /api/goals/{goalId}/tasks` - Create task
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Delete task
- `PATCH /api/tasks/{id}/complete` - Toggle complete
- `POST /api/tasks/{id}/subtasks` - Add subtask

### Categories
- `GET /api/categories` - List categories
- `POST /api/categories` - Create category
- `PUT /api/categories/{id}` - Update category
- `DELETE /api/categories/{id}` - Delete category

## Building for Production

### Frontend
```bash
cd frontend
npm run build
```

Static files will be in `frontend/build/`

### Backend
```bash
cd backend
dotnet publish src/HelpMotivateMe.Api -c Release -o publish
```

## Environment Variables

### Backend (appsettings.json)
- `ConnectionStrings:DefaultConnection` - PostgreSQL connection string
- `Cors:AllowedOrigins` - Allowed frontend origins
- `OAuth:GitHub:ClientId` - GitHub OAuth client ID
- `OAuth:GitHub:ClientSecret` - GitHub OAuth client secret

### Frontend
- `VITE_API_URL` - Backend API URL (default: `http://localhost:5000`)
