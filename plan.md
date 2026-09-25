# Self-Hosted Single-User Monolith Plan

## 1. Purpose

Convert Help Motivate Me from a multi-user SaaS application into a self-hosted,
single-user application that is easy to install, start, operate, back up, and
upgrade on one machine or server.

This document is both the implementation plan and the controlling prompt for
future AI agents. Agents must follow the decisions, boundaries, dependencies,
acceptance criteria, and validation gates below.

## 2. Target Outcome

The completed application has:

- One repository, one production image, one process, one HTTP origin, and one
  persistent data directory.
- An ASP.NET Core host that serves the API, the built Svelte SPA, uploaded
  files, and health endpoints.
- SQLite instead of PostgreSQL.
- Exactly one application user, provisioned from configuration.
- Username/password login with cookie authentication.
- No registration, email verification, passwordless login, password reset,
  OAuth, social login, waitlist, whitelist, memberships, payments, buddies,
  user administration, email delivery, or web push delivery.
- A first-run experience requiring only a username and password. OpenAI remains
  optional and the core product works without an API key.
- A single `docker compose up -d` production startup path and a documented
  non-Docker startup path.
- One mounted `/data` directory containing all mutable application state.

"Monolith" means one deployable runtime artifact. Do not collapse the existing
Core, Infrastructure, and API projects into one `.csproj` during this migration.
That is unrelated source churn and may be reconsidered only after the monolith
is working.

## 3. Product Scope

### Keep

- Login/logout and the current authenticated cookie model.
- Onboarding, identities, identity proofs, goals, habit stacks, tasks, today
  view, journal, local journal images, personal analytics, and milestones.
- English and Danish localization for retained UI.
- PWA installability and ordinary service-worker caching, if they work without
  push configuration.
- Optional AI onboarding, command bar, and transcription. Missing OpenAI
  configuration must disable or hide AI controls without breaking startup.
- Profile display name, language, onboarding state, and password change.

### Remove

- Public registration and signup controls.
- Email verification, magic login links, email templates, SMTP, and Mailpit.
- GitHub, Google, LinkedIn, and Facebook authentication and account linking.
- Waitlist and whitelist features.
- Accountability buddies, invitations, shared journals, buddy reactions, and
  cross-user resource authorization.
- Membership tiers, pricing, upgrade UI, and per-tier feature descriptions.
- Admin user management, signup management, broadcast notifications, and SaaS
  aggregate dashboards.
- Web push subscriptions, VAPID configuration, notification prompts, push
  service-worker handlers, and scheduled notification delivery.
- SaaS landing, pricing, waitlist, contact, privacy, and terms screens where
  they exist only to support a hosted public service. Retain a small About/help
  screen only if it remains useful in the app.
- Cross-user analytics and per-user/global AI spending administration. Retain
  only personal analytics and a simple optional AI limit if needed to prevent
  accidental local spending.

### Explicitly Out Of Scope For The First Self-Hosted Release

- Multiple local accounts, invitations, teams, or role-based administration.
- Public internet exposure automation, TLS certificate issuance, or an
  embedded reverse proxy. Document reverse-proxy requirements instead.
- Horizontal scaling. SQLite deployment is one application replica only.
- Mobile-native apps.
- Automatic import of an existing SaaS PostgreSQL database. See the data
  migration decision below.

## 4. Architecture Decisions

### 4.1 Runtime And Hosting

- Build the SvelteKit static SPA in a Node build stage.
- Copy `frontend/build` into the ASP.NET publish output under `wwwroot`.
- ASP.NET Core serves static files and falls back to `index.html` for non-API
  client routes.
- API routes remain under `/api`; the browser uses same-origin relative URLs.
- Production requires no CORS policy. Development may retain a narrowly scoped
  localhost CORS policy while Vite runs separately.
- Replace the current Traefik + PostgreSQL + Mailpit + migrations + API +
  frontend Compose topology with one application service and one named volume.
- The container runs as a non-root user and exposes one configurable port.
- Add `/health/live` and `/health/ready`; readiness verifies that SQLite can be
  opened and migrations have completed.

### 4.2 Persistent Data Layout

Use one configurable root, defaulting to `/data` in the container:

```text
/data/
  helpmotivateme.db
  uploads/
  keys/                 # only if data-protection keys are moved out of SQLite
```

- SQLite connection default: `Data Source=/data/helpmotivateme.db`.
- Local uploads default: `/data/uploads`.
- Never place the database in the image or an ephemeral working directory.
- Apply EF Core migrations automatically before accepting traffic. Protect
  migration startup with clear logging and fail fast on errors.
- Configure SQLite foreign keys, a busy timeout, and WAL mode where supported.
- Run only one application replica against a database file.
- Use `DateTimeOffset`/UTC consistently and test SQLite date comparisons used by
  analytics, milestones, and scheduled calculations.

### 4.3 Single User And Authentication

Keep a `User` entity and existing `UserId` foreign keys in the first release.
They provide a stable ownership boundary, minimize migration risk, and leave a
possible future multi-user path. Single-user mode means there is exactly one
valid user record, not that ownership columns are removed everywhere.

Configuration contract:

```text
SingleUser__Username=<required>
SingleUser__Password=<required secret>
SingleUser__DisplayName=<optional>
```

- Add strongly typed `SingleUserOptions` with startup validation.
- Username is a real login field; do not use a fake email address as the
  long-term model. Remove or make obsolete the required email semantics.
- Never commit a real password, log it, return it from an endpoint, expose it in
  health output, or bake it into an image.
- Documentation examples use placeholders and recommend Docker secrets or a
  protected `.env` file (`chmod 600`). Ensure `.env` is ignored by Git.
- On first startup, a hosted initializer creates the singleton user with the
  configured username, a one-way password hash, active/verified state, and all
  permissions required for the retained app.
- On later startups, the initializer updates the username/display name and
  rotates the stored hash when configured credentials change. Credential
  rotation must invalidate existing authentication cookies.
- Use ASP.NET Core `PasswordHasher<TUser>` or retain an equally suitable
  password hashing implementation already used by the application. Never store
  or compare plaintext passwords.
- Login errors use one generic response and rate limiting protects the login
  endpoint. Cookies remain HttpOnly, SameSite=Lax by default, Secure when HTTPS
  is detected/configured, and use an explicit expiration.
- Keep authorization on all private API endpoints. Do not make the API
  anonymous merely because there is one user.
- Centralize singleton user resolution. Controllers must not accept arbitrary
  user IDs for retained personal resources.

### 4.4 Database Migration Policy

PostgreSQL EF migrations are provider-specific (`jsonb`, PostgreSQL identity
annotations, and provider type declarations) and must not be replayed on
SQLite.

For the first self-hosted release:

1. Preserve the old PostgreSQL migrations in Git history; do not attempt to
   edit each migration into SQLite syntax.
2. Finish removal of obsolete entities and provider-specific model mappings.
3. Replace the migration set with a reviewed SQLite initial migration for the
   final retained schema.
4. Test creation from an empty directory and migration from every subsequently
   released self-hosted schema.

An existing SaaS-to-self-hosted data importer is a separate work package and is
not required for the first release. Before deleting a production PostgreSQL
deployment, take a backup. If data preservation becomes a requirement, build a
one-shot, tested export/import tool that selects one source user and related
rows; do not improvise cross-provider SQL in application startup.

### 4.5 Backups And Upgrades

- Document a safe backup procedure. The minimum supported procedure is: stop
  the app, copy the entire `/data` directory, start the app.
- Document restore into an empty deployment using the same application version
  first, then upgrade.
- Add an optional consistent online SQLite backup command only if it can be
  tested reliably.
- Image upgrades must retain `/data`, apply migrations once, and preserve login,
  uploads, and data-protection keys.
- Log the application version, database path, and migration status at startup,
  but never secrets.

## 5. Current Codebase Anchors

Future agents must inspect these files before editing their assigned slice:

- Composition root: `backend/src/HelpMotivateMe.Api/Program.cs`
- Runtime configuration: `backend/src/HelpMotivateMe.Api/appsettings.json`
- API packages: `backend/src/HelpMotivateMe.Api/HelpMotivateMe.Api.csproj`
- Infrastructure packages:
  `backend/src/HelpMotivateMe.Infrastructure/HelpMotivateMe.Infrastructure.csproj`
- EF context and mappings:
  `backend/src/HelpMotivateMe.Infrastructure/Data/AppDbContext.cs`
- Existing migrations:
  `backend/src/HelpMotivateMe.Infrastructure/Migrations/`
- User model: `backend/src/HelpMotivateMe.Core/Entities/User.cs`
- Auth API: `backend/src/HelpMotivateMe.Api/Controllers/AuthController.cs`
- Auth implementation:
  `backend/src/HelpMotivateMe.Infrastructure/Services/AuthService.cs`
- Existing admin provisioning:
  `backend/src/HelpMotivateMe.Api/Services/AdminUserSeeder.cs`
- Backend image: `backend/Dockerfile`
- Frontend image: `frontend/Dockerfile`
- Current deployment: `docker-compose.yml`, `traefik.yml`, and
  `traefik-dynamic.yml`
- Frontend auth: `frontend/src/lib/stores/auth.ts`,
  `frontend/src/lib/api/auth.ts`, and `frontend/src/routes/auth/`
- Navigation/layout: `frontend/src/routes/+layout.svelte` and
  `frontend/src/lib/components/layout/`
- Generated API types: `openapi/v1.json`,
  `frontend/src/lib/types/generated/api.d.ts`, and
  `frontend/src/lib/types/api-types.ts`
- Integration host:
  `backend/tests/HelpMotivateMe.IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs`
- Integration database fixture:
  `backend/tests/HelpMotivateMe.IntegrationTests/Infrastructure/DatabaseFixture.cs`

Known provider/removal anchors:

- `UseNpgsql` is registered in `Program.cs` and the integration test factory.
- Npgsql is referenced by both API and Infrastructure projects.
- Tests use `Testcontainers.PostgreSql`.
- Existing migrations contain PostgreSQL identity annotations and `jsonb`.
- `Program.cs` registers SMTP, web push, scheduled notification, buddies, admin,
  OAuth, admin seeding, CORS, and proxy behavior.
- Frontend code has dedicated admin, buddies, waitlist, pricing, registration,
  verification, OAuth callback, push, membership, and shared-journal surfaces.

## 6. Execution Rules For AI Agents

Every agent must:

1. Read this file and the files listed in its work package.
2. Inspect current Git status and preserve unrelated user changes.
3. State one local hypothesis and one falsifying check before editing.
4. Work only in its assigned ownership boundary. Do not opportunistically
   refactor adjacent code.
5. Search source files, tests, configuration, docs, generated types, and
   localization for every removed symbol. Exclude `bin`, `obj`, `build`, and
   generated output unless the work package explicitly owns generated output.
6. Add or update focused tests with each behavior change.
7. Run the narrowest relevant validation immediately after the first edit, then
   run the package completion gate.
8. Report changed files, deleted behavior, commands run, results, unresolved
   risks, and assumptions.
9. Never commit, push, rewrite history, or modify unrelated work unless the
   orchestrator explicitly requests it.
10. Do not hand-edit generated OpenAPI TypeScript declarations. Regenerate them
    in the designated integration package.

Agents deleting features must remove the complete vertical slice: endpoint,
DTO, interface, implementation, entity/mapping, registration, config, frontend
API, store/component/route, localization, documentation, and tests. A green
compile with dead SaaS code left behind is not completion.

## 7. Work Breakdown

### Phase 0: Baseline And Contract

#### P0-A: Capture The Baseline

Owner: orchestration agent. Dependencies: none. Parallel: no.

- [x] Record current `dotnet test`, `npm run check`, `npm run lint`, and
      `npm run build` results without fixing unrelated failures.
- [x] Record current Docker build result.
- [x] Inventory ignored/generated `bin`, `obj`, and `build` artifacts; fix ignore
      rules if generated output is accidentally tracked or recursively copied.
- [x] Create a compact endpoint and route inventory grouped as keep/remove.
- [x] Confirm whether any real PostgreSQL data must be imported. Default is no.
- [x] Add architecture decision notes to this document if implementation facts
      require changing a decision.

Gate: baseline failures and final scope are written down before structural work.

Baseline recorded 2026-09-24 at commit `3b55548`:

- `dotnet test`: failed, 103/103 integration tests, because the PostgreSQL
      Testcontainers resource reaper could not initialize. Compilation succeeded.
- `npm run check`: passed with 0 errors and 60 warnings in 14 files.
- `npm run lint`: failed with 235 errors and 53 warnings. These are existing
      findings and are not a gate on starting the migration.
- `npm run build`: passed. VitePWA warned that `portal-intro.webm` is too large
      to precache and that one prerender glob matched no files.
- `docker compose build`: passed and built the existing API, frontend, and
      migrations images.
- Git tracks no files below `bin`, `obj`, or `build`. Generated directories are
      present locally and correctly ignored. The existing `frontend/package-lock.json`
      was incorrectly ignored; the ignore rule was removed for reproducible builds.
- Backend keep: auth (after contraction), AI, identities, identity proofs,
      goals, habit stacks, tasks, today, journal/files, personal analytics, daily
      commitments, and milestones. Backend remove: admin, waitlist, accountability
      buddies/shared content, and push notifications.
- Frontend keep: login, onboarding, dashboard, today, identities, goals, habit
      stacks, journal, analytics, settings, and a reduced About/help screen.
      Frontend remove: registration/verification/OAuth/buddy-invite auth routes,
      admin, buddies, contact, FAQ, pricing, privacy, terms, and waitlist routes.
- No PostgreSQL data importer is required for the first self-hosted release.
      No baseline fact requires changing an architecture decision in this plan.

### Phase 1: SQLite Foundation

#### P1-A: Provider And Model Compatibility

Owner: database agent. Dependencies: P0-A. Parallel: P1-B after model compiles.

- [ ] Replace Npgsql packages with `Microsoft.EntityFrameworkCore.Sqlite` in the
      owning project; remove duplicate provider references.
- [ ] Change runtime registration from `UseNpgsql` to `UseSqlite` using a
      validated connection string and ensured parent directory.
- [ ] Audit model configuration and LINQ for PostgreSQL-only types/functions,
      especially `jsonb`, provider identity annotations, case-insensitive matching,
      date arithmetic, and ordering/grouping involving timestamps.
- [ ] Store JSON payloads as SQLite `TEXT` with explicit conversions where
      required.
- [ ] Add SQLite connection initialization for foreign keys, busy timeout, and
      WAL mode where EF/connection lifecycle permits.
- [ ] Keep data-protection persistence operational and covered by a restart test.

Validation:

```bash
cd backend
dotnet build HelpMotivateMe.sln
```

#### P1-B: SQLite Test Harness

Owner: test-infrastructure agent. Dependencies: P1-A provider registration
contract. Parallel: yes, with P1-C after coordination.

- [ ] Remove `Testcontainers.PostgreSql` and PostgreSQL fixtures.
- [ ] Use a unique temporary on-disk SQLite database per test collection or
      factory. Do not use EF's InMemory provider because relational behavior matters.
- [ ] Keep test isolation deterministic and delete temporary files on disposal.
- [ ] Remove fake OAuth and mock email setup after those services disappear.
- [ ] Add a migration/startup smoke test using a genuinely empty database file.

Validation:

```bash
cd backend
dotnet test tests/HelpMotivateMe.IntegrationTests
```

#### P1-C: Final SQLite Schema And Migrations

Owner: migration agent. Dependencies: complete after Phases 2 and 3 remove
entities; preparatory audit may run during P1-A.

- [ ] Delete the provider-specific migration set only after the retained EF model
      is final.
- [ ] Generate a new SQLite initial migration; do not hand-author the model
      snapshot.
- [ ] Review tables, foreign keys, delete behavior, indexes, required fields,
      enum conversions, JSON text fields, and data-protection keys.
- [ ] Prove automatic startup migration works on an empty `/data` directory and
      is idempotent on restart.

Gate: no Npgsql/Testcontainers PostgreSQL references outside historical docs,
and all backend tests run without Docker.

### Phase 2: Single-User Authentication

#### P2-A: Backend Credential Model And Provisioning

Owner: backend-auth agent. Dependencies: P1-A. Parallel: P2-B can begin after
endpoint contracts are agreed.

- [ ] Add and validate `SingleUserOptions`.
- [ ] Change the user login identity from required email to required username.
- [ ] Implement idempotent singleton-user startup initialization.
- [ ] Define deterministic handling of an unexpected second user: fail startup
      with remediation guidance rather than silently deleting data.
- [ ] Hash configured passwords and support configuration-driven rotation.
- [ ] Invalidate old cookies after credential rotation.
- [ ] Reduce auth endpoints to login, logout, current user, profile/language,
      password change if it has coherent config semantics, and onboarding completion.
- [ ] Decide one source of truth for password changes. Preferred: configuration
      remains authoritative, so either remove in-app password change or provide a
      secure persisted override with documented precedence. Do not implement two
      silently competing password sources.
- [ ] Remove registration, verification, resend, magic-link, OAuth callback,
      link/unlink, membership mutation, and provider fields from responses.
- [ ] Replace broad `IAuthService` with a small retained interface or focused
      services; remove obsolete methods and token/external-login entities.
- [ ] Keep generic login errors and add endpoint rate limiting.

Required tests:

- Fresh startup provisions one user.
- Restart is idempotent and keeps domain data.
- Correct and incorrect credentials.
- Missing/blank/default credentials fail fast with actionable messages.
- Changed configured credentials rotate access and invalidate old cookies.
- A second user causes a controlled startup failure.
- Anonymous requests to private endpoints return 401.

#### P2-B: Frontend Login And Session

Owner: frontend-auth agent. Dependencies: P2-A endpoint contract. Parallel: yes.

- [ ] Replace email-oriented login labels/state with username/password.
- [ ] Remove register, verification, pending verification, buddy invite, OAuth
      callback, social buttons, linking UI, and magic-link behavior.
- [ ] Simplify auth store error handling and redirects for one login route.
- [ ] Preserve return-to behavior for protected routes.
- [ ] Ensure logout and expired sessions return to login cleanly.
- [ ] Remove email, provider, membership, and role assumptions from user UI/types
      unless a retained feature genuinely uses them.
- [ ] Update English and Danish strings for the retained auth experience.

Validation:

```bash
cd frontend
npm run check
npm run lint
```

Gate: one configured account can log in, use a protected endpoint, log out, and
log in after a process restart; no alternate account creation path exists.

### Phase 3: Remove SaaS And Multi-User Features

These packages may run in parallel only if each agent stays within its listed
vertical slice. Coordinate shared edits to `Program.cs`, `AppDbContext`, `User`,
navigation, localization files, and generated types through the orchestrator.

#### P3-A: Email And Passwordless Removal

Owner: email-removal agent. Dependencies: P2-A retained contracts.

- [ ] Remove SMTP service, `IEmailService`, MailKit, templates, configuration,
      token entities/DTOs/mappings, and all callers.
- [ ] Remove email-specific notification preferences and copy that no retained
      feature uses.
- [ ] Remove Mailpit from Compose and dev-container ports/environment.

#### P3-B: OAuth And External Login Removal

Owner: oauth-removal agent. Dependencies: P2-A retained contracts.

- [ ] Remove four provider registrations, packages, config, callbacks, claims,
      external login entity/mapping/DTOs, frontend helpers, and provider UI.
- [ ] Verify source and deployment files contain no provider secrets/placeholders.

#### P3-C: Buddies And Shared Content Removal

Owner: buddies-removal agent. Dependencies: P2-A singleton identity.

- [ ] Remove accountability buddy controller/service/interface/entity/DTOs.
- [ ] Remove buddy invitations and shared today/journal endpoints.
- [ ] Remove buddy reactions only if they have no single-user journal purpose.
- [ ] Simplify resource authorization to owner-only checks; do not remove
      authorization wholesale.
- [ ] Remove frontend buddy routes/API/store/navigation/journal filters/copy.

#### P3-D: Waitlist, Admin, Membership, And Marketing Removal

Owner: SaaS-removal agent. Dependencies: P2-A singleton provisioning.

- [ ] Remove waitlist/whitelist controllers, services, entities, DTOs, mappings,
      settings, routes, and localization.
- [ ] Remove admin controller/service/dashboard and admin user seeder.
- [ ] Preserve milestone definition seeding if milestones remain, moving any
      necessary management behavior out of the deleted admin surface.
- [ ] Remove roles if nothing retained needs them; otherwise reduce to a single
      internal role without exposing role management.
- [ ] Remove membership tier entity fields/enums/DTO fields/config and pricing UI.
- [ ] Remove hosted-service marketing routes and links listed in Product Scope.
- [ ] Replace the root route with login or the authenticated default route.

#### P3-E: Push And Scheduled Delivery Removal

Owner: notifications-removal agent. Dependencies: none after scope lock.

- [ ] Remove WebPush package, service/interface, controller, subscriptions,
      notification preferences/logs used only for delivery, VAPID config, and
      scheduled delivery workers.
- [ ] Remove frontend push APIs/services/prompts/settings/FAB behavior and push
      event handling from the service worker.
- [ ] Preserve in-app daily commitment and digest UI if it does not require push.

#### P3-F: SaaS Analytics And AI Budget Simplification

Owner: analytics-cleanup agent. Dependencies: P3-D contract.

- [ ] Remove signup/session/admin aggregate analytics used only to operate SaaS.
- [ ] Keep personal progress analytics.
- [ ] Replace per-user/global AI budget administration with an optional simple
      local limit, or remove budget tracking if OpenAI client-side availability and
      error handling already provide a clear disabled state.
- [ ] Ensure no background work loops over users.

Gate for Phase 3:

```bash
rg -n -i \
  'smtp|mailkit|oauth|github|google|linkedin|facebook|waitlist|whitelist|buddy|buddies|vapid|webpush|membershiptier' \
  backend/src frontend/src docker-compose.yml .devcontainer README.md docs \
  -g '!**/bin/**' -g '!**/obj/**' -g '!frontend/src/lib/types/generated/**'
```

Every remaining match must be justified in the agent report. Then run backend
tests and frontend checks.

### Phase 4: One Process And One Image

#### P4-A: Same-Origin Static Hosting

Owner: hosting agent. Dependencies: P2-B and Phase 3 frontend removal.

- [ ] Configure ASP.NET static files, default files, and SPA fallback without
      intercepting `/api`, `/health`, uploaded-file, or OpenAPI routes.
- [ ] Make frontend API requests relative in production.
- [ ] Remove production CORS; retain only the Vite development exception.
- [ ] Ensure cookie settings work on plain HTTP localhost and HTTPS behind a
      reverse proxy using forwarded headers from explicitly trusted proxies/networks.
      Do not retain trust-all proxy configuration.
- [ ] Remove the production dependency on `FrontendUrl`.
- [ ] Decide API docs access for a single user; keep authenticated or development
      only, without an admin-role dependency.
- [ ] Verify direct navigation and browser refresh on nested SPA routes.

#### P4-B: Unified Container And Compose

Owner: container agent. Dependencies: P4-A.

- [ ] Replace separate backend/frontend Dockerfiles with a multi-stage root
      Dockerfile: Node frontend build, .NET restore/build/publish, minimal .NET
      runtime image.
- [ ] Use `npm ci`, lock-file reproducibility, Docker cache-friendly copy order,
      and a non-root runtime user.
- [ ] Create one Compose app service, one port mapping, one `/data` volume, a
      healthcheck, restart policy, and required credential variables.
- [ ] Remove PostgreSQL, Mailpit, migration job, frontend service, Traefik, and
      obsolete volumes/configuration.
- [ ] Add `.env.example` containing placeholders only.
- [ ] Ensure a missing data directory is initialized and a mounted directory has
      correct runtime permissions.

#### P4-C: Local Developer Experience

Owner: developer-experience agent. Dependencies: P1-A and P2-A; coordinate final
commands with P4-B.

- [ ] Add root commands/scripts for restore, check, test, development, build, and
      clean startup. Prefer cross-platform tooling already present in the repo.
- [ ] Keep a fast two-process Vite + API development workflow if hot reload is
      valuable, but provide one root command that starts it.
- [ ] Update dev-container configuration to remove PostgreSQL/Mailpit assumptions
      and unnecessary forwarded ports.
- [ ] Ensure local `dotnet run` uses a user-writable SQLite/upload directory and
      clear development-only credentials without committing a production secret.
- [ ] Remove obsolete migration Dockerfiles/scripts or rewrite them for SQLite.

Gate:

```bash
docker compose build --no-cache
docker compose up -d
docker compose ps
```

From a clean volume, health becomes ready, the SPA loads, login works, retained
data can be created, and data survives `docker compose down` followed by
`docker compose up -d`.

### Phase 5: Contract Regeneration And Full Validation

#### P5-A: API Contract And Frontend Types

Owner: contract agent. Dependencies: all endpoint changes complete.

- [ ] Start the final API and regenerate `openapi/v1.json` using the documented
      command.
- [ ] Regenerate frontend API declarations and update only intentional aliases.
- [ ] Confirm removed endpoints and DTO fields are absent.
- [ ] Run frontend checks after regeneration.

#### P5-B: End-To-End Tests

Owner: end-to-end agent. Dependencies: P4-B and P5-A.

- [ ] Add browser coverage for first login, failed login, protected-route
      redirect, onboarding, one representative CRUD path, logout, and restart.
- [ ] Test desktop and mobile layouts for retained navigation and removed links.
- [ ] Verify service worker/PWA behavior has no push errors.
- [ ] Verify uploads persist and remain readable after restart.
- [ ] Verify an absent OpenAI key does not break startup or core workflows.
- [ ] Verify nested client route refreshes return the SPA, while unknown API
      routes return API 404 responses.

#### P5-C: Security And Operational Review

Owner: review agent. Dependencies: P5-B implementation complete.

- [ ] Search images, Git history additions, logs, config, frontend bundles, and
      OpenAPI output for credential leakage.
- [ ] Verify login rate limiting, cookie flags, forwarded-header trust, upload
      path validation, file permissions, and non-root container execution.
- [ ] Verify database and uploads are not publicly served.
- [ ] Verify health endpoints disclose no secrets or sensitive paths.
- [ ] Exercise backup, restore, and image upgrade procedures.
- [ ] Confirm SQLite single-replica limitation is prominent in docs.

Final validation:

```bash
cd backend && dotnet test
cd ../frontend && npm run check && npm run lint && npm run build
cd .. && docker compose build --no-cache
```

Gate: all checks pass, a clean-machine smoke test passes using only documented
steps, and no removed feature is reachable or referenced in user-facing UI.

### Phase 6: Documentation And Release

#### P6-A: Self-Hosting Documentation

Owner: documentation agent. Dependencies: final commands and config stable.

- [ ] Rewrite the README around self-hosting rather than SaaS development.
- [ ] Add a five-minute quick start with prerequisites, `.env` creation, startup,
      URL, login, stop, logs, and upgrade commands.
- [ ] Document every supported setting, required/optional status, default, and
      secret-handling guidance.
- [ ] Document Docker and non-Docker operation, reverse proxy/TLS, `/data`
      permissions, backups, restore, upgrades, troubleshooting, and SQLite limits.
- [ ] Update architecture/developer docs and remove PostgreSQL, SMTP, OAuth,
      VAPID, Traefik, and SaaS instructions.
- [ ] Update privacy/security text to accurately describe local storage and
      optional data sent to OpenAI.
- [ ] Add release notes that clearly state there is no automatic PostgreSQL data
      migration.

Release gate: a person unfamiliar with the repository can start a fresh instance
and restore a backup by following only the documentation.

## 8. Parallel Agent Schedule

Recommended orchestration waves:

| Wave | Work packages                            | Parallelism                   | Merge/validation point                           |
| ---- | ---------------------------------------- | ----------------------------- | ------------------------------------------------ |
| 0    | P0-A                                     | 1                             | Baseline recorded                                |
| 1    | P1-A                                     | 1                             | Backend compiles on SQLite                       |
| 2    | P1-B, P2-A, P3-E audit                   | 2-3                           | SQLite tests and auth contract pass              |
| 3    | P2-B, P3-A, P3-B, P3-C, P3-D, P3-E, P3-F | Up to 7 with strict ownership | Orchestrator resolves shared files; Phase 3 gate |
| 4    | P1-C, P4-A, P4-C                         | Up to 3                       | Fresh schema and same-origin app pass            |
| 5    | P4-B                                     | 1                             | Clean-volume container smoke test                |
| 6    | P5-A, then P5-B                          | 1-2 in dependency order       | Full contract/E2E pass                           |
| 7    | P5-C, P6-A                               | 2                             | Release gate                                     |

Do not launch several agents that all edit `Program.cs`, `AppDbContext.cs`,
`User.cs`, navigation, or localization files at once. For Wave 3, feature agents
should prepare complete slice changes, while one integration agent owns conflict
resolution and shared composition files.

## 9. Reusable Agent Prompt

Use this template for each work package:

```text
You are implementing work package <ID and title> from plan.md in the
Help Motivate Me repository.

Read all of plan.md first. Treat its target architecture, scope, execution
rules, and package dependencies as requirements. Inspect the package's listed
anchors and current Git status before editing. Preserve unrelated changes.

Your ownership boundary is:
<files/directories/symbols this agent may change>

Do not change:
<shared files owned by the integration agent, unrelated packages, generated
files unless assigned>

Required outcomes:
<copy the package checklist and acceptance criteria>

Before the first edit, state one falsifiable local hypothesis and the cheapest
check that could disprove it. Make the smallest coherent implementation, run a
focused validation immediately, then complete the package validation. Add or
update tests for changed behavior. Search for dangling references to removed
symbols while excluding bin/obj/build artifacts.

Finish with:
- files changed/deleted
- behavior implemented/removed
- tests and commands run with results
- unresolved risks or follow-up work
- any deviation from plan.md and why

Do not commit or push.
```

## 10. Definition Of Done

The migration is complete only when all are true:

- [ ] A fresh clone can be configured and started with one documented command.
- [ ] Production runs one application container/process and one persistent data
      volume, with no external database, mail server, frontend server, or migration
      job.
- [ ] Fresh startup creates a valid SQLite schema and exactly one configured
      user; restart is idempotent.
- [ ] Username/password login is the only authentication path.
- [ ] All private APIs still require authentication.
- [ ] Credential rotation behavior is tested and documented.
- [ ] Core retained workflows work without email, OAuth, push, or OpenAI.
- [ ] Removed SaaS features have no endpoints, services, entities, packages,
      routes, navigation, translations, configuration, or documentation left.
- [ ] PostgreSQL/Npgsql/Testcontainers PostgreSQL are absent from runtime and
      test dependencies.
- [ ] The SPA and API share one origin and nested route refresh works.
- [ ] SQLite, uploads, and data-protection state survive container recreation.
- [ ] Backup, restore, and upgrade procedures have been executed successfully.
- [ ] Backend tests, frontend checks/lint/build, Docker build, and end-to-end
      smoke tests pass.
- [ ] OpenAPI and generated frontend types match the final API.
- [ ] No secrets are committed, logged, included in images, or shipped to the
      browser.
- [ ] Documentation states that the application supports one user and one
      running replica.

## 11. Deferred Follow-Ups

Evaluate only after the first self-hosted release is stable:

- Optional PostgreSQL-to-SQLite one-user import utility.
- Optional local notifications that require no external delivery service.
- Built-in password reset via local console command.
- Further source consolidation into fewer .NET projects.
- Packaging for additional platforms or native installers.
- Reintroducing multiple local users as a separately designed feature.
