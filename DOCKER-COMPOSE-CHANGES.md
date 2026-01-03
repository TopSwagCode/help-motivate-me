# Docker Compose Changes for Traefik + ngrok

## Summary
Configured Traefik as a reverse proxy to enable single-port access for ngrok sharing.

## Key Changes

### 1. Added Traefik Service
```yaml
services:
  traefik:
    image: traefik:v3.0
    ports:
      - "80:80"       # Main entrypoint for ngrok
      - "8080:8080"   # Dashboard
```

**Purpose:** Acts as reverse proxy, routing requests to appropriate services.

### 2. Backend Changes

#### Removed Direct Port Exposure
```yaml
# Before
ports:
  - "5001:5001"

# After  
expose:
  - "5001"
```

#### Added Traefik Labels
```yaml
labels:
  - "traefik.enable=true"
  - "traefik.http.routers.backend.rule=PathPrefix(`/api`)"
```

#### Updated Environment Variables
```yaml
# Before
- LocalStorage__BaseUrl=http://localhost:5001

# After
- LocalStorage__BaseUrl=/api/files
```

**Impact:** Backend now only accessible through Traefik at `/api/*`

### 3. Frontend Changes

#### Removed Direct Port Exposure
```yaml
# Before
ports:
  - "5173:3000"

# After
expose:
  - "3000"
```

#### Added Traefik Labels
```yaml
labels:
  - "traefik.http.routers.frontend.rule=PathPrefix(`/`)"
  - "traefik.http.routers.frontend.priority=1"
```

#### Updated Environment Variables
```yaml
# Before
- VITE_API_URL=http://localhost:5001

# After
- VITE_API_URL=
```

**Impact:** 
- Frontend accessible at `/` 
- API calls use same domain (no CORS issues)
- Lowest priority (catches unmatched routes)

### 4. Mailpit Changes

#### Removed Direct Port Exposure
```yaml
# Before
ports:
  - "8025:8025"
  - "1025:1025"

# After
expose:
  - "8025"
  - "1025"
```

#### Added Traefik Labels
```yaml
labels:
  - "traefik.http.routers.mailpit.rule=PathPrefix(`/mail`)"
  - "traefik.http.middlewares.mailpit-stripprefix.stripprefix.prefixes=/mail"
```

**Impact:** Mailpit UI accessible at `/mail`

## Routing Logic

### Priority Order
1. **Backend** (`/api/*`) - Higher priority (default)
2. **Mailpit** (`/mail`) - Higher priority (default)
3. **Frontend** (`/*`) - Priority 1 (lowest, catches everything else)

### How It Works
```
Request: https://abc123.ngrok.io/api/auth/login
  → Matches: /api/*
  → Routes to: Backend service
  → Backend sees: /api/auth/login

Request: https://abc123.ngrok.io/mail
  → Matches: /mail
  → Strips: /mail prefix
  → Routes to: Mailpit service
  → Mailpit sees: /

Request: https://abc123.ngrok.io/dashboard
  → No specific match
  → Routes to: Frontend (lowest priority)
  → Frontend sees: /dashboard
```

## Network Architecture

```
┌─────────────────────────────────────────────┐
│                  Internet                    │
└──────────────────┬──────────────────────────┘
                   │
                   │ ngrok tunnel
                   │
         ┌─────────▼─────────┐
         │   localhost:80    │
         └─────────┬─────────┘
                   │
         ┌─────────▼─────────────────────┐
         │    Traefik Reverse Proxy      │
         │    (helpmotivateme-network)   │
         └─────────┬─────────────────────┘
                   │
         ┌─────────┼─────────────────┐
         │         │                 │
    ┌────▼────┐ ┌─▼──────┐ ┌───────▼────┐
    │ Frontend│ │Backend │ │  Mailpit   │
    │  :3000  │ │ :5001  │ │   :8025    │
    └─────────┘ └────┬───┘ └────────────┘
                     │
              ┌──────▼──────┐
              │  PostgreSQL  │
              │    :5432     │
              └──────────────┘
```

## Port Mapping

### Before (Direct Access)
| Service | Local Port | External Access |
|---------|-----------|-----------------|
| Frontend | 5173 | localhost:5173 |
| Backend | 5001 | localhost:5001 |
| Mailpit | 8025 | localhost:8025 |
| **Total** | **3 ports** | **3 ngrok tunnels needed** |

### After (Traefik Proxy)
| Service | Internal Port | Route | External Access |
|---------|--------------|-------|-----------------|
| Traefik | 80 | - | localhost:80 |
| Frontend | 3000 | `/` | ngrok.io/ |
| Backend | 5001 | `/api/*` | ngrok.io/api/* |
| Mailpit | 8025 | `/mail` | ngrok.io/mail |
| **Total** | **1 external port** | **1 ngrok tunnel** |

## Benefits

### 1. Single ngrok Tunnel
- **Before:** Need paid ngrok (or 3 separate free accounts)
- **After:** Works with free ngrok tier

### 2. Clean URLs
- **Before:** `ngrok1.io`, `ngrok2.io`, `ngrok3.io`
- **After:** `ngrok.io/`, `ngrok.io/api`, `ngrok.io/mail`

### 3. No CORS Issues
- **Before:** Frontend on different domain than API
- **After:** Same domain for all services

### 4. Production-Ready
- **Before:** Development-only setup
- **After:** Mimics production reverse proxy

### 5. Easier Sharing
- **Before:** Share 3 URLs, explain routing
- **After:** Share 1 URL, everything works

## Files Created

1. **`TRAEFIK-NGROK-SETUP.md`** - Complete setup guide
2. **`TRAEFIK-QUICKREF.md`** - Quick reference card
3. **`start-with-traefik.sh`** - Startup script
4. **`ngrok.yml`** - ngrok configuration template

## Migration Steps

### From Old Setup
1. Stop old containers: `docker-compose down`
2. Update docker-compose.yml (done)
3. Start with Traefik: `./start-with-traefik.sh`
4. Verify: `open http://localhost:8080`
5. Test routes: `curl http://localhost/api/health`
6. Start ngrok: `ngrok http 80`
7. Share ngrok URL

### No Code Changes Needed!
The API client already uses `/api` prefix, so no frontend code changes required.

## Testing Checklist

- [ ] Traefik starts: `docker ps | grep traefik`
- [ ] Dashboard accessible: `open http://localhost:8080`
- [ ] Frontend loads: `curl http://localhost/`
- [ ] API responds: `curl http://localhost/api/...`
- [ ] Mailpit loads: `open http://localhost/mail`
- [ ] ngrok tunnel works: Test all routes via ngrok URL
- [ ] File uploads work: Test via ngrok
- [ ] Emails visible: Check mailpit via ngrok

## Rollback Plan

If you need to go back to the old setup:

```bash
# 1. Stop current setup
docker-compose down

# 2. Revert docker-compose.yml changes
git checkout main -- docker-compose.yml

# 3. Start old way
docker-compose up -d
```

## Production Considerations

This setup is production-ready with modifications:

1. **Replace ngrok with real domain**
2. **Add SSL/TLS certificates**
3. **Add authentication middleware**
4. **Enable rate limiting**
5. **Add monitoring**
6. **Use production-grade security**

Traefik supports all of this natively!

## Support

- Issues? Check `TRAEFIK-NGROK-SETUP.md`
- Quick reference? See `TRAEFIK-QUICKREF.md`
- Traefik docs: https://doc.traefik.io/traefik/
- ngrok docs: https://ngrok.com/docs

## Questions?

Common questions answered in `TRAEFIK-NGROK-SETUP.md`:
- Why Traefik?
- How does routing work?
- What about HTTPS?
- Can I use custom domains?
- How do I add authentication?
- What about rate limiting?
