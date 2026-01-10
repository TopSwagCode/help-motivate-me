# Production Deployment Guide

This guide explains how to set up GitHub Container Registry (GHCR) and deploy Help Motivate Me to local, staging, and production environments.

## Table of Contents
1. [GitHub Container Registry Setup](#github-container-registry-setup)
2. [GitHub Actions Workflow](#github-actions-workflow)
3. [Local Development with GHCR Images](#local-development-with-ghcr-images)
4. [Server Prerequisites](#server-prerequisites)
5. [Staging Deployment](#staging-deployment)
6. [Production Deployment](#production-deployment)
7. [DNS Configuration](#dns-configuration)
8. [Troubleshooting](#troubleshooting)

---

## GitHub Container Registry Setup

GitHub Container Registry (ghcr.io) is included with your GitHub account. The Docker images will be stored privately and linked to your repository.

### 1. Enable GitHub Container Registry

1. Go to your GitHub repository: `https://github.com/TopSwagCode/help-motivate-me`
2. Click on **Settings** → **Actions** → **General**
3. Scroll down to **Workflow permissions**
4. Select **Read and write permissions**
5. Check **Allow GitHub Actions to create and approve pull requests** (optional)
6. Click **Save**

### 2. Package Visibility (Optional)

By default, packages inherit repository visibility. To verify or change:

1. Go to your GitHub profile → **Packages**
2. Find the `help-motivate-me-backend` and `help-motivate-me-frontend` packages (after first build)
3. Click on a package → **Package settings**
4. Under **Danger Zone**, you can change visibility if needed

### 3. Create a Personal Access Token (PAT) for Server Access

Your deployment server needs to authenticate with GHCR to pull images:

1. Go to GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. Click **Generate new token (classic)**
3. Give it a descriptive name: `helpmotivateme-server-pull`
4. Set expiration (recommend 90 days or custom)
5. Select scopes:
   - `read:packages` - Download packages from GitHub Package Registry
6. Click **Generate token**
7. **Copy the token immediately** - you won't see it again!

---

## GitHub Actions Workflow

The workflow file `.github/workflows/docker-publish.yml` is already created. It will:

- **Trigger on:**
  - Push to `master`/`main` branch
  - Git tags (e.g., `v1.0.0`)
  - Pull requests (builds but doesn't push)
  - Manual trigger

- **Build and publish:**
  - `ghcr.io/topswagcode/help-motivate-me-backend`
  - `ghcr.io/topswagcode/help-motivate-me-frontend`

- **Tags generated:**
  - `latest` - Latest from default branch
  - `master` or `main` - Branch name
  - `v1.0.0` - Semantic version tags
  - `sha-abc1234` - Git commit SHA

### Triggering a Build

**Automatic:** Push to master or create a tag:
```bash
# Push to master
git push origin master

# Or create a version tag
git tag v1.0.0
git push origin v1.0.0
```

**Manual:** 
1. Go to repository → **Actions** → **Build and Publish Docker Images**
2. Click **Run workflow** → Select branch → **Run workflow**

---

## Local Development with GHCR Images

Use this setup when you want to quickly test pre-built images locally without building from source.

### 1. Authenticate with GHCR (one-time setup)

```bash
# Login to GHCR with your PAT
echo "YOUR_PAT_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

### 2. Configure Environment

```bash
# Copy example env file
cp .env.local.example .env.local

# Edit if needed (defaults work out of the box)
nano .env.local
```

### 3. Start Services

```bash
# Pull latest images and start
docker compose -f docker-compose.local.yml --env-file .env.local pull
docker compose -f docker-compose.local.yml --env-file .env.local up -d
```

### 4. Access the Application

- **Frontend:** http://localhost
- **Traefik Dashboard:** http://localhost:8080
- **Mailpit (emails):** http://localhost:8025 or http://localhost/mail
- **API:** http://localhost/api

### 5. Stop Services

```bash
docker compose -f docker-compose.local.yml --env-file .env.local down
```

---

## Server Prerequisites

### Required Software

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install -y docker.io docker-compose-v2

# Or install Docker from official repo
curl -fsSL https://get.docker.com | sh

# Add your user to docker group
sudo usermod -aG docker $USER
# Log out and back in for changes to take effect
```

### Authenticate with GitHub Container Registry

On your deployment server:

```bash
# Login to GHCR with your PAT
echo "YOUR_PAT_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin

# Example:
echo "ghp_xxxxxxxxxxxx" | docker login ghcr.io -u TopSwagCode --password-stdin
```


---

## Staging Deployment

### 1. Set Up DNS

Create an A record pointing your staging domain to your server:
- `staging.helpmotivate.me` → `YOUR_SERVER_IP`
- `traefik.staging.helpmotivate.me` → `YOUR_SERVER_IP` (for dashboard)

### 2. Configure Environment

```bash
# Clone repository or copy files to server
cd /opt/helpmotivateme  # or your preferred directory

# Copy example env file
cp .env.staging.example .env.staging

# Edit the configuration
nano .env.staging
```

**Important settings to change:**
- `DOMAIN` - Your staging domain
- `LETSENCRYPT_EMAIL` - Your email for cert notifications
- `POSTGRES_PASSWORD` - Strong database password
- `TRAEFIK_DASHBOARD_AUTH` - Generate secure password
- `OPENAI_API_KEY` - Your OpenAI key

**Generate Traefik dashboard password:**
```bash
# Install htpasswd if needed
sudo apt install apache2-utils

# Generate password hash (escape $ for docker-compose)
echo $(htpasswd -nB admin) | sed -e s/\\$/\\$\\$/g
# Output: admin:$$2y$$05$$...
```

### 3. Deploy

```bash
# Pull latest images
docker compose -f docker-compose.staging.yml --env-file .env.staging pull

# Start services
docker compose -f docker-compose.staging.yml --env-file .env.staging up -d

# Check status
docker compose -f docker-compose.staging.yml --env-file .env.staging ps

# View logs
docker compose -f docker-compose.staging.yml --env-file .env.staging logs -f
```

### 4. Verify

- Visit `https://staging.helpmotivate.me` - Should see frontend (cert warning is expected with staging certs)
- Visit `https://traefik.staging.helpmotivate.me` - Traefik dashboard (login required)
- Check `https://staging.helpmotivate.me/api/health` - Backend health check

⚠️ **Note:** Let's Encrypt staging certificates are NOT trusted by browsers. You'll see security warnings. This is expected and useful for testing the setup before production.

---

## Production Deployment

### 1. Set Up DNS

Create A records:
- `helpmotivate.me` → `YOUR_SERVER_IP`
- `www.helpmotivate.me` → `YOUR_SERVER_IP`
- `traefik.helpmotivate.me` → `YOUR_SERVER_IP` (for dashboard)

### 2. Configure Environment

```bash
# Copy example env file
cp .env.production.example .env.production

# Edit the configuration
nano .env.production
```

**Critical settings:**
- `POSTGRES_PASSWORD` - Use a very strong password
- `LETSENCRYPT_EMAIL` - Must be valid for cert notifications
- `SMTP_*` - Configure real email provider
- `OPENAI_API_KEY` - Production API key
- `TRAEFIK_DASHBOARD_AUTH` - Strong password
- Hetzner backup settings if using remote backups

### 3. Deploy

```bash
# Pull latest images
docker compose -f docker-compose.production.yml --env-file .env.production pull

# Start services
docker compose -f docker-compose.production.yml --env-file .env.production up -d

# Check status
docker compose -f docker-compose.production.yml --env-file .env.production ps
```

### 4. Verify

- Visit `https://helpmotivate.me` - Should see frontend with valid SSL
- Visit `https://www.helpmotivate.me` - Should redirect to non-www
- Check `https://helpmotivate.me/api/health` - Backend health

---

## DNS Configuration

### Example DNS Records (at your registrar)

| Type | Name | Value | TTL |
|------|------|-------|-----|
| A | @ | YOUR_SERVER_IP | 300 |
| A | www | YOUR_SERVER_IP | 300 |
| A | traefik | YOUR_SERVER_IP | 300 |
| A | staging | YOUR_STAGING_IP | 300 |
| A | traefik.staging | YOUR_STAGING_IP | 300 |

---

## Updating Deployments

### Update to Latest

```bash
# Staging
docker compose -f docker-compose.staging.yml --env-file .env.staging pull
docker compose -f docker-compose.staging.yml --env-file .env.staging up -d

# Production
docker compose -f docker-compose.production.yml --env-file .env.production pull
docker compose -f docker-compose.production.yml --env-file .env.production up -d
```

### Update to Specific Version

Edit your `.env.staging` or `.env.production`:
```bash
IMAGE_TAG=v1.2.3  # or sha-abc1234
```

Then redeploy:
```bash
docker compose -f docker-compose.production.yml --env-file .env.production up -d
```

### Rollback

```bash
# Change IMAGE_TAG to previous version
IMAGE_TAG=v1.0.0

# Redeploy
docker compose -f docker-compose.production.yml --env-file .env.production up -d
```

---

## Troubleshooting

### Check Logs

```bash
# All services
docker compose -f docker-compose.production.yml --env-file .env.production logs -f

# Specific service
docker compose -f docker-compose.production.yml --env-file .env.production logs -f backend
docker compose -f docker-compose.production.yml --env-file .env.production logs -f traefik
```

### Certificate Issues

```bash
# Check Traefik logs for ACME/Let's Encrypt errors
docker compose -f docker-compose.production.yml --env-file .env.production logs traefik | grep -i acme

# Check acme.json permissions
docker exec helpmotivateme-traefik cat /letsencrypt/acme.json | head -50
```

### Database Connection Issues

```bash
# Check if postgres is healthy
docker compose -f docker-compose.production.yml --env-file .env.production ps postgres

# Check postgres logs
docker compose -f docker-compose.production.yml --env-file .env.production logs postgres
```

### Can't Pull Images

```bash
# Re-authenticate
echo "YOUR_PAT" | docker login ghcr.io -u YOUR_USERNAME --password-stdin

# Verify authentication
cat ~/.docker/config.json | grep ghcr
```

### Health Checks

```bash
# Backend health
curl -k https://helpmotivate.me/api/health

# Check running containers
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

---

## Security Checklist

Before going live:

- [ ] Change all default passwords
- [ ] Use strong, unique `POSTGRES_PASSWORD`
- [ ] Generate secure `TRAEFIK_DASHBOARD_AUTH`
- [ ] Verify `ALLOW_SIGNUPS` is `false` if you don't want public registration
- [ ] Configure real SMTP for production emails
- [ ] Set up remote backups (Hetzner or other)
- [ ] Consider setting up a firewall (ufw)
- [ ] Enable automatic security updates on server

---

## Quick Reference

```bash
# Start local (using GHCR images)
docker compose -f docker-compose.local.yml --env-file .env.local up -d

# Start staging
docker compose -f docker-compose.staging.yml --env-file .env.staging up -d

# Start production
docker compose -f docker-compose.production.yml --env-file .env.production up -d

# Stop
docker compose -f docker-compose.production.yml --env-file .env.production down

# View logs
docker compose -f docker-compose.production.yml --env-file .env.production logs -f

# Update
docker compose -f docker-compose.production.yml --env-file .env.production pull
docker compose -f docker-compose.production.yml --env-file .env.production up -d
```
