# Generating TypeScript Types from the Backend API

The frontend uses TypeScript types that are auto-generated from the backend's OpenAPI spec. This ensures type safety between the frontend and backend stays in sync.

## Overview

```
Backend DTOs  -->  OpenAPI spec (JSON)  -->  TypeScript types  -->  Frontend
                   openapi/v1.json           generated/api.d.ts
```

The pipeline:

1. The .NET backend exposes an OpenAPI spec at `/openapi/v1.json` (dev mode)
2. A Python script post-processes the spec to fix .NET 10 quirks
3. `openapi-typescript` generates TypeScript types from the spec
4. `api-types.ts` re-exports generated types with friendly frontend names

## Prerequisites

- Backend running locally on `http://localhost:5001` (for spec fetching)
- Python 3 (for the post-processing script)
- Node.js / npm (frontend dependencies installed)

## Quick Start

### If you changed a backend DTO or controller response type:

```bash
# 1. Make sure the backend is running
cd backend
docker compose up -d
dotnet run --project src/HelpMotivateMe.Api

# 2. Fetch the latest spec and regenerate types
cd frontend
npm run generate:spec    # Fetches spec from running backend
npm run generate:types   # Generates TypeScript types

# 3. Verify
npm run check            # Should show 0 errors
```

### If only the spec changed (no backend changes):

```bash
cd frontend
npm run generate:types
npm run check
```

## NPM Scripts

| Script | What it does |
|--------|-------------|
| `npm run generate:spec` | Fetches `openapi/v1.json` from the running backend and runs the fix script |
| `npm run generate:types` | Runs the fix script on the existing spec, then generates `generated/api.d.ts` |

## File Structure

```
project root/
├── openapi/
│   └── v1.json                          # OpenAPI spec (committed to repo)
├── scripts/
│   └── fix-openapi-spec.py              # Post-processing script
└── frontend/src/lib/types/
    ├── generated/
    │   └── api.d.ts                     # Auto-generated (DO NOT EDIT)
    ├── api-types.ts                     # Friendly aliases + frontend-only types
    ├── index.ts                         # Barrel export
    └── tour.ts                          # Frontend-only type (not from API)
```

## How It Works

### 1. OpenAPI Spec (`openapi/v1.json`)

The spec is fetched from the running backend and committed to the repo. This means:
- Frontend can regenerate types without running the backend
- API changes show as diffs in PRs
- CI can validate types against the committed spec

### 2. Post-Processing (`scripts/fix-openapi-spec.py`)

.NET 10's OpenAPI generator has two quirks that the fix script corrects:

**Integer/string union types**: .NET 10 outputs `type: ["integer", "string"]` for int fields (to support large number string representation). The script removes the `"string"` from these unions since the API actually sends numbers.

**Enum serialization mismatch**: The backend uses `JsonStringEnumConverter` so enums are sent as strings at runtime (e.g. `"Completed"`), but the OpenAPI spec describes them as integers. The script replaces these with proper string enum schemas.

Currently patched enums:
- `DailyCommitmentStatus`: Committed, Completed, Dismissed, Missed
- `TaskItemStatus`: Pending, InProgress, Completed, Cancelled
- `ProofIntensity`: Easy, Moderate, Hard

### 3. Type Generation (`openapi-typescript`)

Generates a `.d.ts` file with all schemas as TypeScript types. The flags used:
- `--root-types` exports types at the top level (not just nested under `components`)
- `--root-types-no-schema-prefix` removes the `Schema` prefix from type names

### 4. Type Aliases (`api-types.ts`)

Maps generated backend names to friendly frontend names:

```typescript
// Generated name: UserResponse  -->  Frontend name: User
export type { UserResponse as User } from './generated/api';

// Generated name: GoalResponse  -->  Frontend name: Goal
export type { GoalResponse as Goal } from './generated/api';
```

This file also contains:
- Frontend-only enums (e.g. `MembershipTier`, `Language`)
- Frontend-only types (e.g. `JournalReactionSummary`, `PaginatedResponse<T>`)
- Constants (e.g. `NotificationDays`, `DayOfWeek`)

## Common Tasks

### Adding a new backend DTO

1. Create the DTO in `Core/DTOs/`
2. Use it in a controller response
3. Run `npm run generate:spec` and `npm run generate:types`
4. Add a re-export line in `api-types.ts`:
   ```typescript
   export type { MyNewResponse as MyNew } from './generated/api';
   ```
5. Import in frontend code: `import type { MyNew } from '$lib/types';`

### Adding a new backend enum

If the enum uses `JsonStringEnumConverter`:

1. Create the enum in `Core/Enums/`
2. Add it to `STRING_ENUMS` in `scripts/fix-openapi-spec.py`:
   ```python
   STRING_ENUMS = {
       ...
       "MyNewEnum": ["Value1", "Value2", "Value3"],
   }
   ```
3. Regenerate: `npm run generate:spec && npm run generate:types`

### Renaming or removing a backend field

1. Make the backend change
2. Regenerate types
3. Run `npm run check` -- TypeScript will flag every frontend file that used the old field name

This is the main benefit of generated types: field renames and removals are caught at compile time.

## Troubleshooting

### `generate:spec` returns empty or errors

Make sure the backend is running in development mode:
```bash
cd backend
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/HelpMotivateMe.Api
```
The OpenAPI endpoint is only available in dev mode.

### Generated types have `number | string` unions

The fix script didn't run. Make sure to use `npm run generate:types` (which runs the fix script automatically) rather than calling `openapi-typescript` directly.

### Enum shows as `number` instead of string values

Add the enum to `STRING_ENUMS` in `scripts/fix-openapi-spec.py` with the correct string values matching the C# enum member names.

### `npm run check` shows errors after regenerating

Common causes:
- **`string | undefined` vs `string | null`**: Generated types use `null`. Change `|| undefined` to `|| null` in frontend code.
- **Missing required fields**: A new required field was added to a DTO. Add the field with a default value in the frontend call sites.
- **Type renamed**: Update the alias in `api-types.ts` or the import in the consuming file.
