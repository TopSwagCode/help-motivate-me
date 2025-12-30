# Help Motivate Me

A comprehensive habit tracking and identity-based motivation application built with SvelteKit and .NET. Inspired by James Clear's *Atomic Habits*, this app helps you build better habits through identity reinforcement, habit stacking, and journaling.

## Tech Stack

- **Frontend**: SvelteKit 5 (static SPA) + Tailwind CSS + TypeScript
- **Backend**: .NET 9/10 Web API
- **Database**: PostgreSQL with Entity Framework Core
- **Storage**: AWS S3 (LocalStack for development)
- **Email**: SMTP (Mailpit for development)
- **Authentication**: Cookie-based with GitHub OAuth support

## Prerequisites

- Node.js 18+
- .NET 9+ SDK
- Docker & Docker Compose (for local services)

## Getting Started

### 1. Start Development Services

```bash
cd backend
docker compose up -d
```

This starts:
- **PostgreSQL** on `localhost:5432` (Database: `helpmotivateme`, User: `postgres`, Password: `postgres`)
- **Mailpit** on `localhost:8025` (Web UI for email testing)
- **LocalStack** on `localhost:4566` (S3-compatible storage)

### 2. Run Database Migrations

The API automatically applies migrations on startup in development mode. Alternatively, run manually:

```bash
cd backend
dotnet ef database update --project src/HelpMotivateMe.Infrastructure --startup-project src/HelpMotivateMe.Api
```

### 3. Configure GitHub OAuth (Optional)

1. Create a GitHub OAuth App at [GitHub Developer Settings](https://github.com/settings/developers)
   - Homepage URL: `http://localhost:5173`
   - Authorization callback URL: `http://localhost:5000/signin-github`
2. Update `backend/src/HelpMotivateMe.Api/appsettings.Development.json`:

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

API available at `http://localhost:5000` (with OpenAPI/Swagger at `/openapi` in development)

### 5. Start the Frontend

```bash
cd frontend
npm install
npm run dev
```

App available at `http://localhost:5173`

## Project Structure

```
help-motivate-me/
├── backend/
│   ├── docker-compose.yml              # PostgreSQL, Mailpit, LocalStack
│   ├── HelpMotivateMe.sln
│   ├── src/
│   │   ├── HelpMotivateMe.Api/         # Web API Controllers
│   │   ├── HelpMotivateMe.Core/        # Entities, DTOs, Interfaces
│   │   └── HelpMotivateMe.Infrastructure/  # EF Core, Repositories
│   └── tests/
│       └── HelpMotivateMe.IntegrationTests/
└── frontend/
    ├── src/
    │   ├── lib/
    │   │   ├── api/                    # API client
    │   │   ├── stores/                 # Svelte stores
    │   │   ├── types/                  # TypeScript types
    │   │   └── components/             # UI components
    │   └── routes/                     # SvelteKit pages
    │       ├── (app)/                  # Protected routes
    │       ├── auth/                   # Auth pages
    │       ├── dashboard/
    │       ├── goals/
    │       ├── habit-stacks/
    │       ├── identities/
    │       ├── journal/
    │       ├── today/
    │       ├── analytics/
    │       └── settings/
    └── package.json
```

## Core Features

### 🎯 Identity-Based Motivation
Define who you want to become (e.g., "Writer", "Athlete", "Healthy Person") and link your habits and tasks to these identities. Get reinforcement messages like "That's what a Writer does!" when completing tasks.

### 🔗 Habit Stacking
Build powerful habit chains by stacking small actions together with trigger cues (e.g., "After I make coffee → Meditate for 5 minutes → Write morning pages"). Track daily completions for each step in your stack.

### 📊 Today View
Your daily dashboard showing:
- Active habit stacks with completion status
- Upcoming tasks (due today or pending)
- Completed tasks
- Identity-based feedback and reinforcement

### 📝 Journal
Reflect on your progress with journal entries that can:
- Link to specific habit stacks or tasks
- Include multiple images (up to 5 per entry, stored in S3)
- Track mood, energy levels, and notes
- Filter by date and linked items

### 📈 Analytics & Streaks
Track your progress over time (note: streak tracking is under development).

### ✅ Goals & Tasks
Traditional goal and task management with:
- Repeatable tasks (daily, weekly, monthly)
- Subtasks support
- Priority levels

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login with credentials
- `POST /api/auth/logout` - Logout
- `GET /api/auth/me` - Get current user
- `GET /api/auth/external/github` - Initiate GitHub OAuth
- `POST /api/auth/email-login` - Request passwordless email login
- `GET /api/auth/verify-email-login` - Verify email login token

### Identities
- `GET /api/identities` - List user identities
- `POST /api/identities` - Create identity
- `GET /api/identities/{id}` - Get identity details
- `PUT /api/identities/{id}` - Update identity
- `DELETE /api/identities/{id}` - Delete identity

### Habit Stacks
- `GET /api/habit-stacks` - List habit stacks
- `POST /api/habit-stacks` - Create habit stack
- `GET /api/habit-stacks/{id}` - Get habit stack
- `PUT /api/habit-stacks/{id}` - Update habit stack
- `DELETE /api/habit-stacks/{id}` - Delete habit stack
- `POST /api/habit-stacks/{id}/reorder` - Reorder stacks
- `POST /api/habit-stacks/items/{itemId}/complete` - Toggle completion

### Journal
- `GET /api/journal` - List journal entries
- `POST /api/journal` - Create journal entry
- `GET /api/journal/{id}` - Get journal entry
- `PUT /api/journal/{id}` - Update journal entry
- `DELETE /api/journal/{id}` - Delete journal entry
- `POST /api/journal/{id}/images` - Upload image
- `DELETE /api/journal/images/{imageId}` - Delete image

### Today View
- `GET /api/today?date={date}` - Get today's view (defaults to current date)

### Goals & Tasks
- `GET /api/goals` - List goals
- `POST /api/goals` - Create goal
- `GET /api/goals/{id}` - Get goal
- `PUT /api/goals/{id}` - Update goal
- `DELETE /api/goals/{id}` - Delete goal
- `GET /api/tasks` - List tasks
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Delete task

### Analytics
- `GET /api/analytics/streaks` - Get streak data
- `GET /api/analytics/completion-rates` - Get completion statistics

## Building for Production

### Frontend
```bash
cd frontend
npm run build
# Static files output to frontend/build/
```

### Backend
```bash
cd backend
dotnet publish src/HelpMotivateMe.Api -c Release -o publish
```

## Environment Configuration

### Backend (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=helpmotivateme;Username=postgres;Password=postgres"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  },
  "OAuth": {
    "GitHub": {
      "ClientId": "",
      "ClientSecret": ""
    }
  },
  "S3": {
    "UseLocalStack": true,
    "ServiceUrl": "http://localhost:4566",
    "BucketName": "help-motivate-me",
    "AccessKey": "test",
    "SecretKey": "test"
  },
  "Smtp": {
    "Host": "localhost",
    "Port": 1025,
    "FromEmail": "noreply@helpmotivateme.com",
    "FromName": "Help Motivate Me"
  }
}
```

### Frontend
Create `.env` file:
```env
VITE_API_URL=http://localhost:5000
```

## Development Tools

- **Mailpit**: View sent emails at `http://localhost:8025`
- **LocalStack**: S3-compatible storage for local development
- **OpenAPI**: API documentation at `http://localhost:5000/openapi` (dev mode)

## Contributing

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture documentation and design decisions.
