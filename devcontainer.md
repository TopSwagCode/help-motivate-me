# Dev Container

The dev container provides .NET 10, Node.js 22, Python 3.13, Docker Compose, the EF Core CLI, and the recommended VS Code extensions.

## Prerequisites

Install these on the host machine:

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Visual Studio Code](https://code.visualstudio.com/)
- The VS Code **Dev Containers** extension (`ms-vscode-remote.remote-containers`)

Start Docker Desktop before opening the container.

## Open the project in the dev container

1. Open the repository folder in VS Code.
2. Open the Command Palette with `Cmd+Shift+P` on macOS or `Ctrl+Shift+P` on Windows/Linux.
3. Run **Dev Containers: Reopen in Container**.
4. Wait for the container setup to finish.

On first creation, the setup automatically restores the .NET solution and EF Core tool, then installs the frontend npm packages.

If the container configuration changes, run **Dev Containers: Rebuild and Reopen in Container** from the Command Palette.

## Start development

The recommended workflow runs PostgreSQL and Mailpit in Docker while the API and frontend run natively inside the dev container. Open three VS Code terminals.

### Terminal 1: supporting services

```bash
cd backend
docker compose up -d postgres mailpit
```

### Terminal 2: backend API

```bash
cd backend
dotnet run --project src/HelpMotivateMe.Api
```

### Terminal 3: frontend

```bash
cd frontend
npm run dev -- --host 0.0.0.0
```

Open the frontend at [http://localhost:5173](http://localhost:5173).

## URLs

| Service | URL | Purpose |
| --- | --- | --- |
| Frontend | [http://localhost:5173](http://localhost:5173) | SvelteKit application |
| API | [http://localhost:5001](http://localhost:5001) | Backend API |
| API documentation | [http://localhost:5001/api/docs](http://localhost:5001/api/docs) | Scalar API explorer |
| OpenAPI specification | [http://localhost:5001/api/openapi/v1.json](http://localhost:5001/api/openapi/v1.json) | Generated API schema |
| Mailpit | [http://localhost:8025](http://localhost:8025) | Development email inbox |
| PostgreSQL | `localhost:5432` | Local database connection |

VS Code forwards these ports automatically. Use the **Ports** panel if a forwarded service receives a different host port.

## Full Docker stack

As an alternative, run the complete application through Docker Compose from the repository root:

```bash
docker compose up --build
```

Then open:

- Application: [http://localhost](http://localhost)
- Mailpit: [http://localhost:8025](http://localhost:8025)
- Traefik dashboard: [http://localhost:8080](http://localhost:8080)

Stop the full stack with:

```bash
docker compose down
```

## Stop native development services

Stop the API and frontend with `Ctrl+C` in their terminals. Then stop PostgreSQL and Mailpit:

```bash
cd backend
docker compose down
```

To remove the development database volume as well, use `docker compose down -v`. This permanently deletes local database data.
