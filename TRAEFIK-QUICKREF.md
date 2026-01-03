# Traefik + ngrok Quick Reference

## 🚀 Quick Start

```bash
# 1. Start services
./start-with-traefik.sh

# 2. Start ngrok
ngrok http 80

# 3. Share the ngrok URL!
```

## 📍 Routes

| Path | Service | Description |
|------|---------|-------------|
| `/` | Frontend | SvelteKit web app |
| `/api/*` | Backend | ASP.NET Core API |
| `/mail` | Mailpit | Email testing UI |
| `:8080` | Traefik | Dashboard (localhost only) |

## 🔧 Useful Commands

```bash
# Start everything
docker-compose up -d --build

# Stop everything
docker-compose down

# View logs
docker-compose logs -f
docker-compose logs -f traefik
docker-compose logs -f backend

# Restart a service
docker-compose restart backend

# Rebuild a service
docker-compose up -d --build backend

# Check service health
docker ps
```

## 🌐 ngrok Commands

```bash
# Basic tunnel (free)
ngrok http 80

# With authentication
ngrok http 80 --basic-auth="user:pass"

# With custom subdomain (paid)
ngrok http 80 --subdomain=helpmotivateme

# View active tunnels
ngrok tunnels list

# Stop tunnel
# Press Ctrl+C
```

## 🐛 Troubleshooting

```bash
# Check if Traefik sees services
open http://localhost:8080

# Test routes locally
curl http://localhost/
curl http://localhost/api/health
open http://localhost/mail

# Check backend logs for errors
docker logs helpmotivateme-backend

# Restart Traefik
docker-compose restart traefik
```

## 🔗 Access URLs

### Local Development
```
http://localhost/           # Frontend
http://localhost/api/*      # Backend API
http://localhost/mail       # Mailpit UI
http://localhost:8080       # Traefik Dashboard
```

### Via ngrok (example)
```
https://abc123.ngrok.io/           # Frontend
https://abc123.ngrok.io/api/*      # Backend API
https://abc123.ngrok.io/mail       # Mailpit UI
```

## 📝 Environment Variables

### Frontend
- `VITE_API_URL=` (empty - uses same domain)

### Backend
- `LocalStorage__BaseUrl=/api/files` (relative URLs)

### Mailpit
- No changes needed - uses internal network

## ✅ Checklist

Before sharing via ngrok:

- [ ] All services running: `docker ps`
- [ ] Frontend loads: `curl http://localhost/`
- [ ] API responds: `curl http://localhost/api/health`
- [ ] Mailpit loads: `open http://localhost/mail`
- [ ] Traefik dashboard shows services: `open http://localhost:8080`
- [ ] Start ngrok: `ngrok http 80`
- [ ] Test ngrok URL in browser
- [ ] Share URL with team/client

## 🎯 Common Issues

### "Bad Gateway" Error
```bash
# Check if backend is running
docker ps | grep backend
docker logs helpmotivateme-backend

# Restart backend
docker-compose restart backend
```

### API Calls Not Working
```bash
# Check Traefik routing
open http://localhost:8080

# Verify backend container is on correct network
docker inspect helpmotivateme-backend | grep Network
```

### Mailpit Not Loading
```bash
# Check if mailpit is running
docker ps | grep mailpit

# Check Traefik labels
docker inspect helpmotivateme-mailpit | grep traefik
```

### Services Not in Traefik Dashboard
```bash
# Rebuild with labels
docker-compose up -d --build

# Check logs
docker logs helpmotivateme-traefik
```

## 💡 Pro Tips

1. **Keep ngrok Running:** Open in separate terminal
2. **Bookmark Dashboard:** http://localhost:8080
3. **Test Locally First:** Before using ngrok
4. **Use ngrok Web Interface:** http://127.0.0.1:4040
5. **Check Logs:** When in doubt, check docker logs

## 🔒 Security Notes

For public sharing:
- Use ngrok basic auth: `--basic-auth="user:pass"`
- Don't share production credentials
- Use temporary test data
- Monitor ngrok dashboard for requests
- Stop ngrok when done

## 📚 More Info

- Full guide: `TRAEFIK-NGROK-SETUP.md`
- Traefik docs: https://doc.traefik.io/traefik/
- ngrok docs: https://ngrok.com/docs
