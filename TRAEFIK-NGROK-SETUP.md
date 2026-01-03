# Traefik + ngrok Setup Guide

## Overview
This setup uses Traefik as a reverse proxy to route traffic from a single domain (via ngrok) to multiple services:
- `/` → Frontend (SvelteKit)
- `/api/*` → Backend (ASP.NET Core API)
- `/mail` → Mailpit Web UI

## Architecture

```
Internet → ngrok → localhost:80 → Traefik → Services
                                     ├─ / → Frontend (3000)
                                     ├─ /api/* → Backend (5001)
                                     └─ /mail → Mailpit (8025)
```

## Quick Start

### 1. Start Services
```bash
docker-compose up -d --build
```

### 2. Verify Traefik is Running
```bash
# Check Traefik dashboard (optional)
open http://localhost:8080

# Check services are registered
curl http://localhost/
curl http://localhost/api/health  # If you have a health endpoint
```

### 3. Start ngrok
```bash
# Install ngrok if you haven't
brew install ngrok

# Start ngrok tunnel to port 80
ngrok http 80
```

### 4. Access Your Application
ngrok will provide a URL like: `https://abc123.ngrok.io`

- **Frontend:** `https://abc123.ngrok.io/`
- **API:** `https://abc123.ngrok.io/api/*`
- **Mailpit:** `https://abc123.ngrok.io/mail`

## Configuration Details

### Traefik Configuration
- **Port 80:** Main HTTP entrypoint (for ngrok)
- **Port 8080:** Traefik dashboard (for debugging)
- **Provider:** Docker with automatic service discovery
- **Network:** All services communicate via `helpmotivateme-network`

### Service Routing

#### Frontend (Priority: 1 - Lowest)
```yaml
labels:
  - "traefik.http.routers.frontend.rule=PathPrefix(`/`)"
  - "traefik.http.routers.frontend.priority=1"
```
Catches all routes not matched by other services.

#### Backend (Default Priority: Higher)
```yaml
labels:
  - "traefik.http.routers.backend.rule=PathPrefix(`/api`)"
```
Handles all `/api/*` routes.

#### Mailpit (Default Priority: Higher)
```yaml
labels:
  - "traefik.http.routers.mailpit.rule=PathPrefix(`/mail`)"
  - "traefik.http.middlewares.mailpit-stripprefix.stripprefix.prefixes=/mail"
```
- Routes `/mail` to Mailpit
- Strips `/mail` prefix before forwarding (so Mailpit sees `/` instead of `/mail`)

## Environment Variables

### Backend Changes
```yaml
environment:
  - LocalStorage__BaseUrl=/api/files  # Changed from http://localhost:5001
```
File URLs now use relative paths that work through Traefik.

### Frontend Changes
```yaml
environment:
  - VITE_API_URL=  # Empty - uses same domain (no CORS issues!)
```
Frontend now makes API calls to the same domain (e.g., `https://abc123.ngrok.io/api/*`).

## Benefits

### 1. Single Domain
All services accessible from one domain - no CORS issues!

### 2. Clean URLs
- `yourdomain.com/` - Frontend
- `yourdomain.com/api/auth/login` - API
- `yourdomain.com/mail` - Mailpit

### 3. Easy Sharing
Share one ngrok URL that provides access to everything.

### 4. Production-Like
Mimics production setup with reverse proxy.

## ngrok Commands

### Basic Tunnel
```bash
ngrok http 80
```

### Custom Subdomain (requires paid ngrok)
```bash
ngrok http 80 --subdomain=helpmotivateme
# Access at: https://helpmotivateme.ngrok.io
```

### With Authentication
```bash
ngrok http 80 --basic-auth="user:password"
```

### With Custom Domain (requires paid ngrok)
```bash
ngrok http 80 --hostname=app.yourdomain.com
```

## Testing the Setup

### 1. Test Frontend
```bash
curl http://localhost/
# Or via ngrok
curl https://abc123.ngrok.io/
```

### 2. Test API
```bash
curl http://localhost/api/health
# Or via ngrok
curl https://abc123.ngrok.io/api/auth/me
```

### 3. Test Mailpit
```bash
# Open in browser
open http://localhost/mail
# Or via ngrok
open https://abc123.ngrok.io/mail
```

### 4. Check Traefik Dashboard
```bash
open http://localhost:8080
```
You should see all services registered with their routing rules.

## Troubleshooting

### Services Not Showing in Traefik
```bash
# Check service labels
docker inspect helpmotivateme-backend | grep traefik

# Check if service is on correct network
docker inspect helpmotivateme-backend | grep helpmotivateme-network

# Check Traefik logs
docker logs helpmotivateme-traefik
```

### API Calls Failing
1. **Check backend is running:**
   ```bash
   docker logs helpmotivateme-backend
   ```

2. **Test direct connection:**
   ```bash
   docker exec helpmotivateme-backend curl http://localhost:5001/api/health
   ```

3. **Check Traefik routing:**
   Visit `http://localhost:8080` and verify backend router is registered.

### Mailpit Not Loading
1. **Check stripprefix middleware:**
   ```bash
   # Should show middleware is attached
   docker logs helpmotivateme-traefik | grep mailpit
   ```

2. **Test direct access:**
   ```bash
   docker exec helpmotivateme-mailpit wget -O- http://localhost:8025
   ```

### Frontend Not Loading
1. **Check if frontend is running:**
   ```bash
   docker logs helpmotivateme-frontend
   ```

2. **Verify it's last priority:**
   Frontend should have `priority=1` so other routes are matched first.

## Security Considerations

### For Public Internet Access

1. **Add Authentication:**
   ```bash
   # Option 1: ngrok basic auth
   ngrok http 80 --basic-auth="username:password"
   ```

2. **Use ngrok IP Restrictions (paid feature):**
   ```bash
   ngrok http 80 --cidr-allow=YOUR_IP/32
   ```

3. **Add Traefik Authentication:**
   You can add BasicAuth or OAuth middleware to Traefik.

4. **Use HTTPS:**
   ngrok provides HTTPS by default, but for production use proper certificates.

## Development Workflow

### Local Development (without ngrok)
```bash
# Access via localhost
http://localhost/          # Frontend
http://localhost/api/*     # Backend
http://localhost/mail      # Mailpit
```

### Share with Team/Client (with ngrok)
```bash
# Start ngrok
ngrok http 80

# Share the ngrok URL
https://abc123.ngrok.io
```

### Multiple Developers
Each developer can run their own ngrok tunnel pointing to their local instance.

## Production Considerations

For production, replace ngrok with:
- Real domain + DNS
- SSL certificates (Let's Encrypt)
- Proper authentication
- Rate limiting
- Monitoring

Traefik supports all of this out of the box!

## Advanced Configuration

### Custom Headers
```yaml
labels:
  - "traefik.http.middlewares.custom-headers.headers.customrequestheaders.X-Custom-Header=value"
  - "traefik.http.routers.backend.middlewares=custom-headers"
```

### HTTPS Redirect (for production)
```yaml
labels:
  - "traefik.http.routers.frontend.entrypoints=web"
  - "traefik.http.routers.frontend.middlewares=redirect-to-https"
  - "traefik.http.middlewares.redirect-to-https.redirectscheme.scheme=https"
```

### Rate Limiting
```yaml
labels:
  - "traefik.http.middlewares.rate-limit.ratelimit.average=100"
  - "traefik.http.middlewares.rate-limit.ratelimit.burst=50"
  - "traefik.http.routers.backend.middlewares=rate-limit"
```

## Useful Commands

```bash
# Stop everything
docker-compose down

# Rebuild and restart
docker-compose up -d --build

# View logs
docker-compose logs -f traefik
docker-compose logs -f backend
docker-compose logs -f frontend

# Check routing rules
curl http://localhost:8080/api/http/routers

# Test API through Traefik
curl http://localhost/api/health

# Stop ngrok
# Press Ctrl+C in the terminal running ngrok
```

## Cost Considerations

### ngrok Free Tier
- 1 online process
- Random URLs (e.g., `abc123.ngrok.io`)
- HTTPS included
- 40 connections/minute
- Perfect for development and demos

### ngrok Paid Plans
- Custom subdomains
- Reserved domains
- IP whitelisting
- More connections
- Multiple tunnels

For occasional sharing, the free tier works great!

## Comparison: Before vs After

### Before (Multiple Ports)
```
http://localhost:5173      → Frontend
http://localhost:5001/api  → Backend
http://localhost:8025      → Mailpit

# Requires 3 ngrok tunnels (paid) or complex setup
```

### After (Single Port with Traefik)
```
http://localhost/          → Frontend
http://localhost/api       → Backend
http://localhost/mail      → Mailpit

# Requires 1 ngrok tunnel (free tier works!)
```

## Summary

✅ Single ngrok tunnel serves all services  
✅ Clean URL structure  
✅ No CORS issues  
✅ Production-like setup  
✅ Easy to share  
✅ Works with ngrok free tier  
✅ Ready for production migration  

Happy sharing! 🚀
