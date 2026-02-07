# Help Motivate Me

![Milo](/assets/milo_repo.png)

A habit tracking and identity-based motivation application built with SvelteKit and .NET. Inspired by James Clear's *Atomic Habits*, this app helps you build better habits through identity reinforcement, habit stacking, journaling, and AI-assisted guidance.

## Tech Stack

- **Frontend**: SvelteKit 5 (static SPA) + Tailwind CSS + TypeScript
- **Backend**: .NET 10 Web API
- **Database**: PostgreSQL with Entity Framework Core
- **Email**: SMTP (Mailpit for development)
- **Authentication**: Cookie-based with OAuth (GitHub, Google, Facebook, LinkedIn) and passwordless email login
- **AI**: OpenAI integration for onboarding, command bar, and voice transcription
- **PWA**: Installable Progressive Web App with offline support
- **i18n**: English and Danish

## Prerequisites

- Node.js 18+
- .NET 10+ SDK
- Docker & Docker Compose (for local services)

## Getting Started

### Quick Start (Docker)

Run the entire stack with a single command from the project root:

```bash
docker compose up --build
```

- **App**: http://localhost
- **Mailpit** (email testing): http://localhost:8025
- **Traefik dashboard**: http://localhost:8080

This starts Traefik as a reverse proxy, PostgreSQL, Mailpit, runs database migrations, and launches both the API and frontend behind a single origin.

### Manual Setup

### 1. Start Development Services

```bash
cd backend
docker compose up -d
```

This starts:
- **PostgreSQL** on `localhost:5432` (Database: `helpmotivateme`, User: `postgres`, Password: `postgres`)
- **Mailpit** on `localhost:8025` (Web UI for email testing)
- **Migrations runner** (auto-applies EF Core migrations)
- **API** on `localhost:5001`

### 2. Run Database Migrations

The API automatically applies migrations on startup in development mode. Alternatively, run manually:

```bash
cd backend
dotnet ef database update --project src/HelpMotivateMe.Infrastructure --startup-project src/HelpMotivateMe.Api
```

### 3. Configure OAuth (Optional)

The app supports GitHub, Google, Facebook, and LinkedIn OAuth. To enable any provider, update `backend/src/HelpMotivateMe.Api/appsettings.Development.json`:

```json
{
  "OAuth": {
    "GitHub": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    },
    "Google": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    },
    "LinkedIn": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    },
    "Facebook": {
      "AppId": "your-app-id",
      "AppSecret": "your-app-secret"
    }
  }
}
```

### 4. Generate VAPID Keys (Optional, for Push Notifications)

Push notifications require VAPID keys. Generate a key pair with:

```bash
npx web-push generate-vapid-keys
```

Add the keys to `backend/src/HelpMotivateMe.Api/appsettings.Development.json`:

```json
{
  "Vapid": {
    "Subject": "mailto:admin@helpmotivateme.app",
    "PublicKey": "your-generated-public-key",
    "PrivateKey": "your-generated-private-key"
  }
}
```

And set the public key in `frontend/.env`:

```env
VITE_VAPID_PUBLIC_KEY=your-generated-public-key
```

The public key must match in both frontend and backend.

### 5. Start the Backend

```bash
cd backend
dotnet run --project src/HelpMotivateMe.Api
```

API available at `http://localhost:5001` (with OpenAPI docs at `/openapi` in development)

### 6. Start the Frontend

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
│   ├── docker-compose.yml              # PostgreSQL, Mailpit, migrations, API
│   ├── HelpMotivateMe.sln
│   ├── src/
│   │   ├── HelpMotivateMe.Api/         # Web API Controllers & Services
│   │   ├── HelpMotivateMe.Core/        # Entities, DTOs, Interfaces
│   │   └── HelpMotivateMe.Infrastructure/  # EF Core, Repositories, Services
│   └── tests/
│       └── HelpMotivateMe.IntegrationTests/
└── frontend/
    ├── src/
    │   ├── lib/
    │   │   ├── api/                    # API client modules
    │   │   ├── components/             # UI components
    │   │   ├── config/                 # App configuration
    │   │   ├── i18n/                   # Translations (en, da)
    │   │   ├── services/               # Frontend services
    │   │   ├── stores/                 # Svelte stores
    │   │   ├── types/                  # TypeScript types
    │   │   └── utils/                  # Helper functions
    │   └── routes/                     # SvelteKit pages
    │       ├── auth/                   # Login, register, OAuth callbacks
    │       ├── dashboard/              # User dashboard
    │       ├── today/                  # Daily view
    │       ├── goals/                  # Goals management
    │       ├── habit-stacks/           # Habit stacking
    │       ├── identities/             # Identity management
    │       ├── journal/                # Journaling
    │       ├── analytics/              # Progress tracking
    │       ├── buddies/                # Accountability buddies
    │       ├── settings/               # User settings
    │       ├── onboarding/             # Guided onboarding
    │       ├── admin/                  # Admin dashboard
    │       ├── about/
    │       ├── faq/
    │       ├── pricing/
    │       ├── contact/
    │       ├── waitlist/
    │       ├── privacy/
    │       └── terms/
    └── package.json
```

## Core Features

### Identity-Based Motivation
Define who you want to become (e.g., "Writer", "Athlete", "Healthy Person") and link your habits and tasks to these identities. Get reinforcement messages like "That's what a Writer does!" when completing tasks.

### Identity Proofs
Log evidence of identity alignment with intensity levels. Track how your daily actions reinforce the person you're becoming.

### Habit Stacking
Build powerful habit chains by stacking small actions together with trigger cues (e.g., "After I make coffee -> Meditate for 5 minutes -> Write morning pages"). Track daily completions for each step in your stack.

### Today View
Your daily dashboard showing:
- Active habit stacks with completion status
- Upcoming tasks (due today or pending)
- Completed tasks
- Daily identity commitment
- Identity-based feedback and reinforcement

### Daily Identity Commitments
Each day, commit to one identity and a specific action that reinforces it. Get suggestions based on your existing habits and tasks.

### Journal
Reflect on your progress with journal entries that can:
- Link to specific habit stacks or tasks
- Include multiple images (up to 5 per entry)
- Track mood, energy levels, and notes
- Filter by date and linked items

### Milestones
Earn achievements as you build habits and make progress. Milestone definitions are managed by admins and awarded automatically based on user activity.

### Accountability Buddies
Invite friends as accountability buddies. Buddies can view your Today page, read your journal, write journal entries for you, and react to your entries.

### Analytics and Streaks
Track your progress over time with streak data and completion rate statistics.

### Goals and Tasks
Traditional goal and task management with:
- Repeatable tasks (daily, weekly, monthly)
- Subtasks support
- Priority levels

### AI Features
- **Command Bar (Cmd+K)**: AI-powered quick actions for creating goals, habits, and identities
- **AI Onboarding**: Guided setup through conversational AI
- **Voice Input**: Audio transcription for hands-free journaling and onboarding

### Push Notifications
Web push notification support with scheduled reminders. Admins can send targeted or broadcast notifications.

### PWA Support
Installable as a Progressive Web App with offline capabilities and a native-like experience.

### Multi-Language
Available in English and Danish with full i18n support.

### Admin Dashboard
Admin panel with:
- User management (search, toggle active, update roles)
- User activity monitoring
- Waitlist and whitelist management
- Analytics overview (events, sessions, users)
- AI usage statistics and logs
- Push notification management
- Milestone definition management

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

### Identity Proofs
- `GET /api/identity-proofs` - Get proofs for a date range
- `POST /api/identity-proofs` - Create a proof
- `DELETE /api/identity-proofs/{id}` - Delete a proof

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

### Goals and Tasks
- `GET /api/goals` - List goals
- `POST /api/goals` - Create goal
- `GET /api/goals/{id}` - Get goal
- `PUT /api/goals/{id}` - Update goal
- `DELETE /api/goals/{id}` - Delete goal
- `GET /api/tasks` - List tasks
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Delete task

### Daily Commitments
- `GET /api/daily-commitment` - Get commitment for a date
- `GET /api/daily-commitment/options` - Get identity options with scores
- `GET /api/daily-commitment/suggestions` - Get action suggestions for an identity
- `GET /api/daily-commitment/yesterday` - Get yesterday's commitment
- `POST /api/daily-commitment` - Create a commitment
- `PUT /api/daily-commitment/{id}/complete` - Mark commitment as completed
- `PUT /api/daily-commitment/{id}/dismiss` - Dismiss a commitment

### Milestones
- `GET /api/milestones` - Get awarded milestones
- `GET /api/milestones/unseen` - Get unseen milestones
- `POST /api/milestones/mark-seen` - Mark milestones as seen
- `GET /api/milestones/stats` - Get user stats

### Accountability Buddies
- `GET /api/buddies` - Get all buddy relationships
- `POST /api/buddies/invite` - Invite a buddy by email
- `DELETE /api/buddies/{id}` - Remove a buddy
- `DELETE /api/buddies/leave/{ownerUserId}` - Leave as someone's buddy
- `GET /api/buddies/{targetUserId}/today` - View buddy's Today page
- `GET /api/buddies/{targetUserId}/journal` - View buddy's journal
- `POST /api/buddies/{targetUserId}/journal` - Write entry for buddy
- `POST /api/buddies/{targetUserId}/journal/{entryId}/reactions` - React to buddy's entry

### Analytics
- `GET /api/analytics/streaks` - Get streak data
- `GET /api/analytics/completion-rates` - Get completion statistics

### AI
- `POST /api/ai/onboarding/chat` - AI onboarding chat (streaming)
- `POST /api/ai/onboarding/transcribe` - Transcribe audio to text
- `POST /api/ai/general/chat` - AI command bar chat (streaming)
- `GET /api/ai/context` - Get AI context data
- `POST /api/ai/general/create-identity` - Create identity from AI recommendation

### Push Notifications
- `POST /api/notifications/push/subscribe` - Subscribe to push notifications
- `DELETE /api/notifications/push/unsubscribe` - Unsubscribe
- `GET /api/notifications/push/status` - Get subscription status

### Files
- `GET /api/files/{filepath}` - Retrieve uploaded files

### Waitlist
- `POST /api/waitlist` - Sign up for the waitlist
- `GET /api/waitlist/check` - Check whitelist status

### Admin (requires admin role)
- `GET /api/admin/stats` - Platform statistics
- `GET /api/admin/stats/daily` - Daily stats
- `GET /api/admin/users` - List users with filtering
- `PATCH /api/admin/users/{userId}/toggle-active` - Toggle user active status
- `PATCH /api/admin/users/{userId}/role` - Update user role
- `GET /api/admin/users/{userId}/activity` - Get user activity
- `GET /api/admin/settings` - Get signup settings
- `GET /api/admin/waitlist` - Get waitlist entries
- `DELETE /api/admin/waitlist/{id}` - Remove from waitlist
- `POST /api/admin/waitlist/{id}/approve` - Approve waitlist entry
- `GET /api/admin/whitelist` - Get whitelist entries
- `POST /api/admin/whitelist` - Add to whitelist
- `DELETE /api/admin/whitelist/{id}` - Remove from whitelist
- `GET /api/admin/analytics/overview` - Analytics overview
- `GET /api/admin/ai-usage/stats` - AI usage statistics
- `GET /api/admin/ai-usage` - AI usage logs
- `POST /api/notifications/push/admin/send-to-user` - Send notification to user
- `POST /api/notifications/push/admin/send-to-all` - Broadcast notification
- `GET /api/notifications/push/admin/users` - Users with push status
- `GET /api/notifications/push/admin/stats` - Push notification stats

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

All configuration sections used by the backend:

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| **ConnectionStrings** | `DefaultConnection` | `Host=localhost;Database=helpmotivateme;...` | PostgreSQL connection string |
| **FrontendUrl** | _(root)_ | `http://localhost:5173` | Base URL of the frontend (used in emails, invite links) |
| **Auth** | `AllowSignups` | `true` | Enable/disable new user registration |
| **Cors** | `AllowedOrigins` | `["http://localhost:5173"]` | Allowed CORS origins (array) |
| **OAuth:GitHub** | `ClientId`, `ClientSecret` | `""` | GitHub OAuth credentials |
| **OAuth:Google** | `ClientId`, `ClientSecret` | `""` | Google OAuth credentials |
| **OAuth:LinkedIn** | `ClientId`, `ClientSecret` | `""` | LinkedIn OAuth credentials |
| **OAuth:Facebook** | `AppId`, `AppSecret` | `""` | Facebook OAuth credentials |
| **Email** | `SmtpHost` | `localhost` | SMTP server hostname |
| | `SmtpPort` | `1025` | SMTP server port (465=SSL, 587=StartTLS, other=none) |
| | `SmtpUsername` | `""` | SMTP auth username (optional, leave empty for local dev) |
| | `SmtpPassword` | `""` | SMTP auth password (optional, leave empty for local dev) |
| | `FromEmail` | `noreply@helpmotivateme.local` | Sender email address |
| | `FromName` | `Help Motivate Me` | Sender display name |
| **OpenAI** | `ApiKey` | `""` | OpenAI API key (required for AI features: onboarding, command bar, voice transcription) |
| **LocalStorage** | `BasePath` | `/app/uploads` | Directory for uploaded files (journal images) |
| | `BaseUrl` | `/api/files` | Public URL prefix for serving uploaded files |
| **Vapid** | `Subject` | `mailto:admin@helpmotivateme.app` | VAPID contact URI for web push |
| | `PublicKey` | _(placeholder)_ | VAPID public key (generate with `npx web-push generate-vapid-keys`) |
| | `PrivateKey` | _(placeholder)_ | VAPID private key |
| **AiBudget** | `GlobalLimitLast30DaysUsd` | `5.0` | Max AI spend across all users in 30 days (USD) |
| | `PerUserLimitLast30DaysUsd` | `0.25` | Max AI spend per user in 30 days (USD) |

### Frontend (`.env`)

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_URL` | _(empty, uses relative URLs)_ | Backend API base URL (e.g. `http://localhost:5001`) |
| `VITE_VAPID_PUBLIC_KEY` | _(none)_ | VAPID public key for push notifications (must match backend) |

## Development Tools

- **Mailpit**: View sent emails at `http://localhost:8025`
- **OpenAPI**: API documentation at `http://localhost:5001/openapi` (dev mode)

## Privacy & Data Philosophy

- We do not use tracking cookies.
- We do not use analytics services.
- We do not sell or share user data.
- Authentication uses a single secure session cookie.
- Users can delete their data at any time.
- AI features are optional.

## License

This project is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).

Commercial licenses are available. See COMMERCIAL.md for details.