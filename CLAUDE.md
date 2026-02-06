# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Help Motivate Me is a habit tracking and identity-based motivation application inspired by James Clear's *Atomic Habits*. It helps users build better habits through identity reinforcement, habit stacking, journaling, and AI-assisted guidance.

## Tech Stack

- **Frontend**: SvelteKit 5 (static SPA) + Tailwind CSS + TypeScript
- **Backend**: .NET 10 Web API with Entity Framework Core
- **Database**: PostgreSQL
- **Testing**: xUnit with Testcontainers, FluentAssertions, Bogus
- **Authentication**: Cookie-based with OAuth (GitHub, Google, Facebook, LinkedIn) and passwordless email login
- **AI**: OpenAI integration for onboarding, command bar, and voice transcription
- **PWA**: Progressive Web App with service worker (via @vite-pwa/sveltekit)
- **i18n**: English and Danish translations

## Common Commands

### Full Stack (Docker)
```bash
docker compose up --build           # Start entire stack
docker compose up -d                # Start in background
```

### Backend Development
```bash
cd backend
docker compose up -d                            # Start PostgreSQL, Mailpit, migrations, API
dotnet run --project src/HelpMotivateMe.Api     # Run API
dotnet watch run --project src/HelpMotivateMe.Api  # Run with hot reload
dotnet test                                     # Run all tests
dotnet test tests/HelpMotivateMe.IntegrationTests  # Run integration tests
```

### Frontend Development
```bash
cd frontend
npm install
npm run dev           # Start dev server (http://localhost:5173)
npm run build         # Build for production
npm run check         # TypeScript/Svelte type checking
npm run check:watch   # Type checking in watch mode
```

### Database Migrations
```bash
cd backend
# Apply migrations (also auto-applies on API startup in development)
dotnet ef database update --project src/HelpMotivateMe.Infrastructure --startup-project src/HelpMotivateMe.Api

# Generate new migration (uses Docker for SDK compatibility)
./generate-migration.sh MigrationName
```

## Architecture

### Backend Structure (Clean Architecture)

```
backend/src/
├── HelpMotivateMe.Api/          # Web API layer
│   ├── Controllers/             # REST endpoints (17 controllers)
│   ├── Services/                # API-specific services (seeders, scheduled tasks)
│   └── Program.cs               # App configuration
├── HelpMotivateMe.Core/         # Domain layer (no dependencies)
│   ├── Entities/                # Domain models
│   ├── DTOs/                    # Data transfer objects
│   ├── Interfaces/              # Repository/service contracts
│   └── Enums/                   # Domain enumerations
└── HelpMotivateMe.Infrastructure/  # Data access layer
    ├── Data/                    # DbContext
    ├── Migrations/              # EF Core migrations
    └── Services/                # Interface implementations (AI, notifications, storage, etc.)
```

### Frontend Structure

```
frontend/src/
├── lib/
│   ├── api/                     # API client modules
│   ├── components/              # Reusable UI components
│   ├── config/                  # App configuration
│   ├── i18n/                    # Internationalization (en, da)
│   ├── services/                # Frontend services
│   ├── stores/                  # Svelte stores for state
│   ├── types/                   # TypeScript type definitions
│   └── utils/                   # Helper functions
└── routes/                      # SvelteKit file-based routing
    ├── auth/                    # Login, register, OAuth, email verification
    ├── dashboard/               # User dashboard
    ├── today/                   # Daily view
    ├── goals/                   # Goals management
    ├── habit-stacks/            # Habit stacking feature
    ├── identities/              # Identity management
    ├── journal/                 # Journaling feature
    ├── analytics/               # Progress tracking
    ├── buddies/                 # Accountability buddies
    ├── settings/                # User settings
    ├── onboarding/              # Guided onboarding (form + AI)
    ├── admin/                   # Admin dashboard
    ├── about/                   # About page
    ├── faq/                     # FAQ page
    ├── pricing/                 # Pricing page
    ├── contact/                 # Contact page
    ├── waitlist/                # Waitlist signup
    ├── privacy/                 # Privacy policy
    └── terms/                   # Terms of service
```

### Key Domain Concepts

- **Identities**: Who the user wants to become (e.g., "Writer", "Athlete") with scoring
- **Identity Proofs**: Evidence of identity alignment with intensity levels
- **Habit Stacks**: Chains of small habits with trigger cues
- **Daily Commitments**: Daily identity commitment with suggested actions
- **Journal Entries**: Can link to habit stacks/tasks, include images (local file storage)
- **Milestones**: Achievement system awarded based on user activity
- **Accountability Buddies**: Social accountability with shared visibility and reactions
- **Today View**: Daily dashboard with active stacks, tasks, and commitments
- **AI Features**: OpenAI-powered onboarding, command bar (Cmd+K), voice transcription
- **Push Notifications**: Web push with scheduled delivery and admin broadcasting

## Development URLs

- Frontend: http://localhost:5173
- Backend API: http://localhost:5001
- OpenAPI/Swagger: http://localhost:5001/openapi (dev mode)
- Mailpit (email testing): http://localhost:8025
- PostgreSQL: localhost:5432 (database: `helpmotivateme`, user: `postgres`, password: `postgres`)

## Environment Configuration

Frontend: Create `.env` with `VITE_API_URL=http://localhost:5001`

Backend: Configure via `appsettings.Development.json` (SMTP, OAuth settings for GitHub/Google/LinkedIn/Facebook)
