# Frontend URL Configuration

## Overview

The `FrontendUrl` environment variable controls the base URL used in email templates (login links, password resets, etc.). This is essential for generating correct links when hosting the application.

## Configuration

### Docker Compose

In `docker-compose.yml`, set the `FrontendUrl` environment variable for the backend service:

```yaml
backend:
  environment:
    - FrontendUrl=https://your-domain.com
```

### Current Configuration

Currently set to: `https://8175f826ee97.ngrok-free.app`

### When to Update

Update this value when:
- **Using ngrok**: Set to your ngrok URL (e.g., `https://abc123.ngrok-free.app`)
- **Deploying to staging**: Set to your staging URL (e.g., `https://staging.helpmotivateme.com`)
- **Deploying to production**: Set to your production URL (e.g., `https://helpmotivateme.com`)
- **Local development**: Set to `http://localhost` (when using Traefik) or `http://localhost:5173` (direct)

### Fallback Behavior

If `FrontendUrl` is not set, the application falls back to:
1. First CORS allowed origin (`Cors:AllowedOrigins:0`)
2. Default: `http://localhost:5173`

## Example Configurations

### Local Development with Traefik
```yaml
- FrontendUrl=http://localhost
```

### Local Development without Traefik
```yaml
- FrontendUrl=http://localhost:5173
```

### ngrok Tunnel
```yaml
- FrontendUrl=https://8175f826ee97.ngrok-free.app
```

### Production
```yaml
- FrontendUrl=https://helpmotivateme.com
```

## How It Works

When a user requests a login link:
1. Backend generates a unique token
2. Builds URL: `{FrontendUrl}/auth/login?token={token}`
3. Sends email with this URL
4. User clicks link and is taken to the frontend
5. Frontend sends token back to backend API for validation

## Testing

After updating the `FrontendUrl`:

1. Restart the backend:
   ```bash
   docker compose up -d --build backend
   ```

2. Request a login link via email

3. Check Mailpit at `http://localhost:8025` or `http://localhost/mail`

4. Verify the login URL in the email matches your `FrontendUrl`

## Related Files

- `docker-compose.yml` - Environment variable configuration
- `backend/src/HelpMotivateMe.Api/appsettings.json` - Default configuration
- `backend/src/HelpMotivateMe.Api/Controllers/AuthController.cs` - URL generation logic
