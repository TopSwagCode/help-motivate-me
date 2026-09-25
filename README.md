# Help Motivate Me

Help Motivate Me is a self-hosted, single-user habit and identity tracker. One ASP.NET Core process serves the API, static Svelte application, uploaded files, and health endpoints. Mutable state lives in one Docker volume backed by SQLite.

## Quick Start

Requirements: Docker Engine with Docker Compose.

```bash
cp .env.example .env
chmod 600 .env
# Edit .env and replace SINGLE_USER_USERNAME and SINGLE_USER_PASSWORD.
docker compose up -d --build
```

Open <http://localhost:8080> and sign in with the configured credentials.

```bash
docker compose logs -f app    # logs
docker compose down           # stop; data is retained
docker compose up -d          # start again
```

Do not commit `.env`. The application rejects missing credentials, passwords shorter than 12 characters, and the placeholder `change-me`.

## Configuration

| Variable | Required | Default | Purpose |
| --- | --- | --- | --- |
| `SINGLE_USER_USERNAME` | Yes | none | Login username |
| `SINGLE_USER_PASSWORD` | Yes | none | Login password; use a long random value |
| `SINGLE_USER_DISPLAY_NAME` | No | empty | Display name |
| `PORT` | No | `8080` | Published host port |
| `OPENAI_API_KEY` | No | empty | Enables optional AI-backed features |

Configuration changes to the username or password are applied at restart. Credential changes increment the account credential version and invalidate existing authentication cookies. Configuration is authoritative, so password changes are not offered in the application.

The `help_motivate_me_helpmotivateme_data` volume is mounted at `/data` and contains:

```text
/data/
  helpmotivateme.db
  uploads/
```

Only one application replica may use a SQLite database file.

## Backup And Restore

Use a stopped backup to guarantee a consistent database and upload snapshot:

```bash
docker compose down
docker run --rm \
  -v help-motivate-me_helpmotivateme_data:/data:ro \
  -v "$PWD":/backup \
  alpine tar czf /backup/help-motivate-me-data.tar.gz -C /data .
docker compose up -d
```

Restore into an empty volume with the same application version first:

```bash
docker compose down
docker volume rm help-motivate-me_helpmotivateme_data
docker volume create help-motivate-me_helpmotivateme_data
docker run --rm \
  -v help-motivate-me_helpmotivateme_data:/data \
  -v "$PWD":/backup:ro \
  alpine tar xzf /backup/help-motivate-me-data.tar.gz -C /data
docker compose up -d
```

## Upgrade

Back up `/data`, then pull or build the new image and recreate the service:

```bash
docker compose down
docker compose build --pull
docker compose up -d
```

Check `docker compose ps` and <http://localhost:8080/health/ready>. Never run two replicas against the same volume.

## Non-Docker Development

Requirements: .NET 10 SDK and Node.js 22.

```bash
npm --prefix frontend ci
npm --prefix frontend run dev
```

In another terminal:

```bash
ConnectionStrings__DefaultConnection='Data Source=../../../data/helpmotivateme.db;Foreign Keys=True;Default Timeout=30' \
SingleUser__Username=admin \
SingleUser__Password='development-only-password' \
dotnet run --project backend/src/HelpMotivateMe.Api
```

The Vite app runs at <http://localhost:5173> and the API at <http://localhost:5001>. For a production-style local build, run `npm --prefix frontend run build`, copy `frontend/build` to the API publish directory as `wwwroot`, and run the published API.

## Health And Reverse Proxy

- `/health/live` confirms the process is running.
- `/health/ready` confirms SQLite is reachable.

Terminate TLS at a reverse proxy and forward requests to port 8080. Forward `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host`, and configure ASP.NET's known proxy/network list for your deployment. Do not expose the container directly to the public internet without TLS and access controls.

## Data Migration Notice

There is no automatic PostgreSQL-to-SQLite migration. Back up any existing PostgreSQL deployment before replacing it. Importing one SaaS user and related records requires a separate tested export/import tool.

## Development Checks

```bash
dotnet test backend/HelpMotivateMe.sln
npm --prefix frontend run check
npm --prefix frontend run lint
npm --prefix frontend run build
SINGLE_USER_USERNAME=admin SINGLE_USER_PASSWORD=development-only-password docker compose build
```

The project is licensed under the terms in [LICENSE](LICENSE). See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidance.
