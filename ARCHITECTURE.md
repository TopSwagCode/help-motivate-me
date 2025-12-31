# Help Motivate Me - Architecture Documentation

A modern TODO/task/motivation web application built with SvelteKit and .NET Core 10.

---

## Table of Contents

1. [Project Structure](#project-structure)
2. [Tech Stack](#tech-stack)
3. [Database](#database)
4. [Authentication](#authentication)
5. [API Endpoints](#api-endpoints)
6. [Development Setup](#development-setup)
7. [Production Deployment](#production-deployment)

---

## Project Structure

```
help-motivate-me/
├── backend/
│   ├── docker-compose.yml              # PostgreSQL container
│   ├── HelpMotivateMe.sln
│   └── src/
│       ├── HelpMotivateMe.Api/         # Web API (Controllers, Middleware)
│       │   ├── Controllers/
│       │   │   ├── AuthController.cs   # Authentication endpoints
│       │   │   ├── GoalsController.cs  # Goal CRUD
│       │   │   ├── TasksController.cs  # Task management
│       │   │   └── CategoriesController.cs
│       │   ├── Program.cs              # App configuration
│       │   └── appsettings.json
│       │
│       ├── HelpMotivateMe.Core/        # Domain layer (no dependencies)
│       │   ├── Entities/               # EF Core entities
│       │   │   ├── User.cs
│       │   │   ├── Goal.cs
│       │   │   ├── TaskItem.cs
│       │   │   ├── Category.cs
│       │   │   ├── RepeatSchedule.cs
│       │   │   └── UserExternalLogin.cs
│       │   ├── Enums/
│       │   │   ├── RepeatFrequency.cs  # Daily, Weekly, Monthly
│       │   │   └── TaskStatus.cs       # Pending, InProgress, Completed
│       │   ├── Interfaces/             # Repository contracts
│       │   └── DTOs/                   # Request/Response objects
│       │       ├── Auth/
│       │       ├── Goals/
│       │       └── Tasks/
│       │
│       └── HelpMotivateMe.Infrastructure/  # Data access layer
│           ├── Data/
│           │   ├── AppDbContext.cs
│           │   ├── Configurations/     # EF Core entity configs
│           │   └── Migrations/
│           └── Services/
│
└── frontend/
    ├── src/
    │   ├── lib/
    │   │   ├── api/                    # API client functions
    │   │   │   ├── client.ts           # Base fetch wrapper
    │   │   │   ├── auth.ts
    │   │   │   ├── goals.ts
    │   │   │   ├── tasks.ts
    │   │   │   └── categories.ts
    │   │   ├── stores/                 # Svelte 5 runes stores
    │   │   │   └── auth.ts
    │   │   ├── types/                  # TypeScript interfaces
    │   │   │   └── index.ts
    │   │   └── components/
    │   │       ├── ui/
    │   │       ├── layout/
    │   │       ├── auth/
    │   │       ├── goals/
    │   │       └── tasks/
    │   ├── routes/
    │   │   ├── +layout.svelte          # Root layout
    │   │   ├── +page.svelte            # Landing page
    │   │   ├── auth/
    │   │   │   ├── login/+page.svelte
    │   │   │   ├── register/+page.svelte
    │   │   │   └── callback/+page.svelte  # OAuth callback
    │   │   ├── dashboard/+page.svelte
    │   │   └── goals/
    │   │       ├── new/+page.svelte
    │   │       └── [id]/+page.svelte
    │   └── app.css                     # Tailwind + global styles
    ├── svelte.config.js                # adapter-static
    ├── tailwind.config.js
    └── package.json
```

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | SvelteKit 2.x (static SPA via adapter-static) |
| Styling | Tailwind CSS |
| Backend | .NET 10 Web API |
| Database | PostgreSQL 15+ |
| ORM | Entity Framework Core 10 |
| Auth | Cookie-based + GitHub OAuth |

### Key Packages

**Backend:**
- `Microsoft.AspNetCore.Authentication.Cookies` - Cookie auth
- `AspNet.Security.OAuth.GitHub` - GitHub OAuth provider
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL driver
- `BCrypt.Net-Next` - Password hashing

**Frontend:**
- `@sveltejs/adapter-static` - Build as static SPA
- `tailwindcss` - Utility CSS framework

---

## Database

### Entity Relationship Diagram

```
┌─────────────┐       ┌──────────────────────┐
│   users     │       │  user_external_logins│
├─────────────┤       ├──────────────────────┤
│ id (PK)     │◄──────│ user_id (FK)         │
│ username    │       │ provider             │
│ email       │       │ provider_user_id     │
│ password    │       └──────────────────────┘
│ display_name│
│ created_at  │
│ updated_at  │
└─────────────┘
      │
      │ 1:N
      ▼
┌─────────────┐       ┌──────────────────────┐
│   goals     │       │    categories        │
├─────────────┤       ├──────────────────────┤
│ id (PK)     │       │ id (PK)              │
│ user_id(FK) │       │ user_id (FK)         │
│ title       │       │ name                 │
│ description │       │ color                │
│ target_date │       │ icon                 │
│ is_completed│       └──────────────────────┘
│ sort_order  │              │
└─────────────┘              │
      │                      │
      │         ┌────────────┴────────────┐
      │         │   goal_categories       │
      │         │   (junction table)      │
      │         └─────────────────────────┘
      │
      │ 1:N
      ▼
┌─────────────────────┐       ┌──────────────────────┐
│    task_items       │       │   repeat_schedules   │
├─────────────────────┤       ├──────────────────────┤
│ id (PK)             │       │ id (PK)              │
│ goal_id (FK)        │       │ frequency            │
│ parent_task_id (FK) │◄──┐   │ interval_value       │
│ title               │   │   │ days_of_week         │
│ description         │   │   │ day_of_month         │
│ status              │   │   │ next_occurrence      │
│ due_date            │   │   │ end_date             │
│ repeat_schedule_id  │───┼──►└──────────────────────┘
│ sort_order          │   │
│ is_repeatable       │   │
└─────────────────────┘   │
         │                │
         └────────────────┘
         (self-ref for subtasks)
```

### Migration Commands

```bash
cd backend

# Create a new migration
dotnet ef migrations add <MigrationName> \
  --project src/HelpMotivateMe.Infrastructure \
  --startup-project src/HelpMotivateMe.Api

# Apply migrations to database
dotnet ef database update \
  --project src/HelpMotivateMe.Infrastructure \
  --startup-project src/HelpMotivateMe.Api

# Remove last migration (if not applied)
dotnet ef migrations remove \
  --project src/HelpMotivateMe.Infrastructure \
  --startup-project src/HelpMotivateMe.Api

# Generate SQL script
dotnet ef migrations script \
  --project src/HelpMotivateMe.Infrastructure \
  --startup-project src/HelpMotivateMe.Api \
  -o migrations.sql
```

### Auto-Migration (Development Only)

The API automatically applies migrations on startup in development mode:

```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```

---

## Authentication

### Overview

The application uses **cookie-based authentication** for both local accounts and OAuth providers. This allows seamless authentication across the SPA frontend and API backend.

### Cookie Configuration

```csharp
// Program.cs
.AddCookie(options =>
{
    options.Cookie.Name = "HelpMotivateMe.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;      // Dev: Lax
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;

    // API returns 401 instead of redirect
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});
```

### Production Cookie Settings

For cross-domain deployment (e.g., `app.example.com` + `api.example.com`):

```csharp
options.Cookie.SameSite = SameSiteMode.None;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.Domain = ".example.com";  // Shared domain
```

### CSRF Protection

All mutating requests (POST, PUT, DELETE, PATCH) require the `X-CSRF: 1` header:

```typescript
// frontend/src/lib/api/client.ts
const response = await fetch(url, {
    method,
    credentials: 'include',  // Send cookies cross-origin
    headers: {
        'Content-Type': 'application/json',
        'X-CSRF': '1'        // CSRF token
    },
    body: JSON.stringify(data)
});
```

### Auth Flows

#### 1. Username/Password Registration

```
Frontend                          Backend
   │                                 │
   │  POST /api/auth/register        │
   │  { username, email, password }  │
   │ ───────────────────────────────►│
   │                                 │ Create user
   │                                 │ Hash password (BCrypt)
   │                                 │ Sign in (set cookie)
   │         Set-Cookie + User       │
   │ ◄───────────────────────────────│
```

#### 2. Username/Password Login

```
Frontend                          Backend
   │                                 │
   │  POST /api/auth/login           │
   │  { username, password }         │
   │ ───────────────────────────────►│
   │                                 │ Verify password
   │                                 │ Sign in (set cookie)
   │         Set-Cookie + User       │
   │ ◄───────────────────────────────│
```

#### 3. GitHub OAuth

```
Frontend                    Backend                        GitHub
   │                           │                              │
   │ Click "Login with GitHub" │                              │
   │ ─────────────────────────►│                              │
   │                           │ Redirect to GitHub OAuth     │
   │ ◄────────────────────────────────────────────────────────│
   │                           │                              │
   │ User authorizes app       │                              │
   │ ────────────────────────────────────────────────────────►│
   │                           │                              │
   │                           │ GET /signin-github?code=...  │
   │                           │◄─────────────────────────────│
   │                           │                              │
   │                           │ Exchange code for user info  │
   │                           │ Find/create user             │
   │                           │ Link external login          │
   │                           │ Sign in (set cookie)         │
   │                           │                              │
   │ Redirect to /auth/callback?success=true                  │
   │ ◄─────────────────────────│                              │
   │                           │                              │
   │ GET /api/auth/me          │                              │
   │ ─────────────────────────►│                              │
   │        User data          │                              │
   │ ◄─────────────────────────│                              │
```

### GitHub OAuth Setup

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Create new OAuth App:
   - **Application name**: Help Motivate Me
   - **Homepage URL**: `http://localhost:5173`
   - **Authorization callback URL**: `http://localhost:5001/signin-github`
3. Copy Client ID and Client Secret
4. Add to `appsettings.Development.json`:

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

### User Linking

When a user logs in via GitHub with an email that already exists:
- The system links the GitHub account to the existing user
- Future GitHub logins will use the same account
- This allows users to use both password and OAuth login

---

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login with credentials |
| POST | `/api/auth/logout` | Clear session cookie |
| GET | `/api/auth/me` | Get current user info |
| GET | `/api/auth/external/github` | Initiate GitHub OAuth |

### Goals

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/goals` | List all goals |
| GET | `/api/goals/{id}` | Get goal details |
| POST | `/api/goals` | Create new goal |
| PUT | `/api/goals/{id}` | Update goal |
| DELETE | `/api/goals/{id}` | Delete goal |
| PATCH | `/api/goals/{id}/complete` | Toggle completion |

### Tasks

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/goals/{goalId}/tasks` | List tasks for goal |
| POST | `/api/goals/{goalId}/tasks` | Create task |
| PUT | `/api/tasks/{id}` | Update task |
| DELETE | `/api/tasks/{id}` | Delete task |
| PATCH | `/api/tasks/{id}/complete` | Toggle completion |
| POST | `/api/tasks/{id}/subtasks` | Add subtask |

### Categories

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/categories` | List categories |
| POST | `/api/categories` | Create category |
| PUT | `/api/categories/{id}` | Update category |
| DELETE | `/api/categories/{id}` | Delete category |

---

## Development Setup

### Prerequisites

- Node.js 18+
- .NET 10 SDK
- Docker (for PostgreSQL)

### 1. Start Database

```bash
cd backend
docker compose up -d
```

PostgreSQL runs on `localhost:5432`:
- Database: `helpmotivateme`
- Username: `postgres`
- Password: `postgres`

### 2. Run Migrations

```bash
cd backend
dotnet ef database update \
  --project src/HelpMotivateMe.Infrastructure \
  --startup-project src/HelpMotivateMe.Api
```

### 3. Start Backend

```bash
cd backend
dotnet run --project src/HelpMotivateMe.Api
```

API available at `http://localhost:5001`

### 4. Start Frontend

```bash
cd frontend
npm install
npm run dev
```

App available at `http://localhost:5173`

### Development URLs

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5173 |
| Backend API | http://localhost:5001 |
| PostgreSQL | localhost:5432 |

---

## Production Deployment

### Building Frontend

```bash
cd frontend
npm run build
```

Output: `frontend/build/` (static files)

The frontend is built as a static SPA that can be served from any static host (Netlify, Vercel, S3, Nginx, etc.)

### Building Backend

```bash
cd backend
dotnet publish src/HelpMotivateMe.Api -c Release -o publish
```

### Environment Variables

**Backend (appsettings.Production.json):**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.example.com;Database=helpmotivateme;Username=app;Password=secret"
  },
  "Cors": {
    "AllowedOrigins": ["https://app.example.com"]
  },
  "OAuth": {
    "GitHub": {
      "ClientId": "production-client-id",
      "ClientSecret": "production-client-secret"
    }
  }
}
```

**Frontend (.env):**

```
VITE_API_URL=https://api.example.com
```

### Production Checklist

- [ ] Update cookie settings for cross-domain (`SameSite=None`, `Secure=true`, `Domain=.example.com`)
- [ ] Configure HTTPS on both frontend and backend
- [ ] Update GitHub OAuth callback URL to production URL
- [ ] Set proper CORS origins
- [ ] Use production PostgreSQL instance
- [ ] Set up database backups

---

## Common Tasks

### Adding a New Entity

1. Create entity in `HelpMotivateMe.Core/Entities/`
2. Create DTOs in `HelpMotivateMe.Core/DTOs/`
3. Add DbSet to `AppDbContext`
4. Create configuration in `Data/Configurations/`
5. Create migration: `dotnet ef migrations add AddNewEntity ...`
6. Create controller in `HelpMotivateMe.Api/Controllers/`
7. Add API functions in `frontend/src/lib/api/`
8. Add types in `frontend/src/lib/types/`

### Adding a New OAuth Provider

1. Install NuGet package (e.g., `AspNet.Security.OAuth.Google`)
2. Add configuration in `Program.cs`:
   ```csharp
   .AddGoogle(options => {
       options.ClientId = config["OAuth:Google:ClientId"];
       options.ClientSecret = config["OAuth:Google:ClientSecret"];
   });
   ```
3. Add endpoint in `AuthController`:
   ```csharp
   [HttpGet("external/google")]
   public IActionResult GoogleLogin() => Challenge(...);
   ```
4. Add callback handling (follows same pattern as GitHub)
5. Add button in frontend login page

---

## Troubleshooting

### Cookie Not Being Set

1. Check browser dev tools → Network → Response Headers for `Set-Cookie`
2. Ensure `credentials: 'include'` in fetch calls
3. In dev: cookies work with `SameSite=Lax`
4. Cross-domain requires `SameSite=None` + `Secure=true` + HTTPS

### OAuth Redirect Error

1. Verify callback URL matches exactly in GitHub settings
2. Check `CallbackPath` in `Program.cs` matches GitHub config
3. Common callback path: `/signin-github`

### Database Connection Failed

1. Ensure Docker is running: `docker ps`
2. Start PostgreSQL: `cd backend && docker compose up -d`
3. Check connection string in `appsettings.json`

### Migration Conflicts

1. If migration fails, check for pending migrations: `dotnet ef migrations list`
2. Remove failed migration: `dotnet ef migrations remove`
3. Fix entity configuration and create new migration
