# GitHub OAuth Setup with ngrok

## The Problem

When using GitHub OAuth with ngrok, GitHub needs to redirect back to your ngrok URL, not `localhost`. This requires proper configuration on both the GitHub side and your application side.

## Current Configuration

Your app is configured with:
- **FrontendUrl**: `https://8175f826ee97.ngrok-free.app`
- **Backend API URL**: `https://8175f826ee97.ngrok-free.app/api`

## GitHub OAuth App Setup

### 1. Go to GitHub Developer Settings

Visit: https://github.com/settings/developers

### 2. Create or Update OAuth App

Click on "OAuth Apps" → "New OAuth App" (or edit existing)

### 3. Configure OAuth App Settings

**For ngrok (Development/Testing):**

- **Application name**: `Help Motivate Me (Dev)`
- **Homepage URL**: `https://8175f826ee97.ngrok-free.app`
- **Authorization callback URL**: `https://8175f826ee97.ngrok-free.app/api/signin-github`

**Important**: The callback URL must be:
```
{YOUR_NGROK_URL}/api/signin-github
```

### 4. Get Your Credentials

After saving, you'll see:
- **Client ID**: Copy this
- **Client Secret**: Click "Generate a new client secret" and copy it

### 5. Update docker-compose.yml

Add the GitHub OAuth credentials to your backend service:

```yaml
backend:
  environment:
    - OAuth__GitHub__ClientId=your_client_id_here
    - OAuth__GitHub__ClientSecret=your_client_secret_here
```

### 6. Restart Backend

```bash
docker compose up -d backend
```

## OAuth Flow

Here's how the OAuth flow works:

1. User clicks "Login with GitHub" on frontend
2. Frontend navigates to: `https://8175f826ee97.ngrok-free.app/api/auth/external/GitHub`
3. Backend redirects to GitHub with callback URL
4. User authorizes on GitHub
5. GitHub redirects to: `https://8175f826ee97.ngrok-free.app/api/auth/callback/GitHub`
6. Backend processes the callback and creates/updates user
7. Backend redirects to: `https://8175f826ee97.ngrok-free.app/auth/callback`
8. Frontend receives auth cookie and redirects to dashboard

## Multiple Environments

You can create separate OAuth apps for different environments:

### Development (Local)
- **Homepage URL**: `http://localhost:5173`
- **Callback URL**: `http://localhost:5001/api/signin-github`

### Development (ngrok)
- **Homepage URL**: `https://your-ngrok-url.ngrok-free.app`
- **Callback URL**: `https://your-ngrok-url.ngrok-free.app/api/signin-github`

### Staging
- **Homepage URL**: `https://staging.helpmotivateme.com`
- **Callback URL**: `https://staging.helpmotivateme.com/api/signin-github`

### Production
- **Homepage URL**: `https://helpmotivateme.com`
- **Callback URL**: `https://helpmotivateme.com/api/signin-github`

## Common Issues

### Error: "redirect_uri_mismatch"

**Cause**: The callback URL in your GitHub OAuth app doesn't match the one being sent.

**Solution**: 
1. Check your GitHub OAuth app callback URL
2. Make sure it matches: `{FrontendUrl}/api/auth/callback/GitHub`
3. Ensure `FrontendUrl` in docker-compose.yml is correct

### Error: "The requested scope exceeds what is granted"

**Cause**: User hasn't granted email permissions.

**Solution**: 
1. User needs to revoke the app authorization on GitHub
2. Try logging in again and grant email scope

### GitHub Callback Goes to localhost

**Cause**: The `FrontendUrl` environment variable isn't set correctly.

**Solution**:
1. Verify `FrontendUrl` in docker-compose.yml
2. Restart backend: `docker compose up -d backend`

### ngrok URL Changes

Every time you restart ngrok (free tier), you get a new URL.

**Solutions**:
1. **Update GitHub OAuth app** with new ngrok URL each time
2. **Update docker-compose.yml** with new ngrok URL
3. **Restart backend**: `docker compose up -d backend`

OR

4. **Use ngrok paid plan** for a static domain
5. Configure GitHub OAuth once with your static ngrok domain

## Testing

1. Open your ngrok URL in a browser: `https://8175f826ee97.ngrok-free.app`
2. Click "Login with GitHub"
3. Should redirect to GitHub authorization page
4. After authorization, should redirect back to your app
5. Check cookies - you should have `.HelpMotivateMe.Auth` cookie
6. Should be logged in and redirected to dashboard

## Environment Variables Reference

```yaml
backend:
  environment:
    # Frontend URL for redirects and emails
    - FrontendUrl=https://8175f826ee97.ngrok-free.app
    
    # GitHub OAuth (get from https://github.com/settings/developers)
    - OAuth__GitHub__ClientId=your_github_client_id
    - OAuth__GitHub__ClientSecret=your_github_client_secret
```

## Security Notes

- ⚠️ **Never commit OAuth secrets** to git
- ⚠️ Use different OAuth apps for dev/staging/prod
- ⚠️ Rotate secrets if they're exposed
- ⚠️ ngrok free URLs are public - anyone with the URL can access your app
- ✅ Consider using ngrok's basic auth: `ngrok http 80 --basic-auth="user:pass"`
