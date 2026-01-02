# Backup & Restore System

Complete backup and restore solution for Help Motivate Me, with support for local testing and production use with Hetzner Storage Box.

## Table of Contents

- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Local Testing](#local-testing)
- [Production Setup (Hetzner)](#production-setup-hetzner)
- [Configuration](#configuration)
- [Manual Operations](#manual-operations)
- [Scheduled Backups](#scheduled-backups)
- [Troubleshooting](#troubleshooting)

---

## Quick Start

### 1. Start Services

```bash
docker-compose up -d
```

The backup service will start automatically and schedule backups based on the cron schedule (default: daily at 2:00 AM).

### 2. Create Manual Backup

```bash
./backup-now.sh
```

### 3. List Backups

```bash
./backup-list.sh
```

### 4. Restore from Backup

```bash
./backup-restore.sh backup_20260102_143000

# Or restore latest:
./backup-restore.sh latest
```

---

## Architecture

### Components

1. **Backup Service** (Docker container)
   - Alpine Linux with PostgreSQL client tools
   - Automated scheduled backups via cron
   - Manual backup/restore scripts
   - SSH/rsync for remote storage

2. **Backup Contents**
   - PostgreSQL database (compressed dump)
   - Uploads directory (tar.gz archive)
   - Metadata file (backup info)

3. **Storage**
   - **Local**: Docker volume `backup_data`
   - **Remote**: Hetzner Storage Box (optional)

### Backup Structure

```
backups/
├── local/
│   ├── backup_20260102_020000.tar.gz
│   ├── backup_20260103_020000.tar.gz
│   └── backup_20260104_020000.tar.gz
```

Each backup archive contains:
```
backup_20260102_020000/
├── database.dump          # PostgreSQL custom format dump
├── uploads.tar.gz         # Compressed uploads directory
└── metadata.txt           # Backup information
```

---

## Local Testing

Perfect for testing backup/restore before deploying to production.

### 1. Start Stack

```bash
docker-compose up -d
```

### 2. Create Test Data

Use your application to create some data (users, journal entries, uploads, etc.)

### 3. Create Backup

```bash
./backup-now.sh
```

Output will show:
- Database backup size
- Uploads backup size
- Total archive size
- Backup location

### 4. Verify Backup

```bash
./backup-list.sh
```

### 5. Test Restore

```bash
# Create more test data
# Then restore to previous state
./backup-restore.sh latest
```

### 6. Verify Restoration

Check that your data was restored to the previous state.

---

## Production Setup (Hetzner)

### Prerequisites

1. **Hetzner Storage Box** account
   - Sign up at https://www.hetzner.com/storage/storage-box
   - Note your username and server address

2. **SSH Key Setup**

Generate SSH key on your server:
```bash
ssh-keygen -t rsa -b 4096 -f ~/.ssh/hetzner_backup
```

Add public key to Hetzner Storage Box:
```bash
cat ~/.ssh/hetzner_backup.pub | ssh -p 23 u123456@u123456.your-storagebox.de "cat >> .ssh/authorized_keys"
```

Test connection:
```bash
ssh -p 23 -i ~/.ssh/hetzner_backup u123456@u123456.your-storagebox.de
```

### Configuration

Update `docker-compose.yml`:

```yaml
backup:
  environment:
    # Enable remote backup
    - REMOTE_BACKUP_ENABLED=true
    
    # Hetzner Storage Box credentials
    - HETZNER_USER=u123456
    - HETZNER_HOST=u123456.your-storagebox.de
    - HETZNER_PORT=23
    - HETZNER_REMOTE_PATH=/backups/helpmotivateme
    - CLEANUP_REMOTE=true
    
    # Retention policy
    - RETENTION_DAYS=30
    - MAX_BACKUPS=10
  
  volumes:
    # Mount SSH key
    - ~/.ssh/hetzner_backup:/root/.ssh/id_rsa:ro
```

### Deploy

```bash
docker-compose up -d backup
```

### Verify Remote Backup

After the first backup (manual or scheduled):

```bash
# Check logs
docker-compose logs backup

# Verify on Hetzner
ssh -p 23 u123456@u123456.your-storagebox.de "ls -lh /backups/helpmotivateme/"
```

### Download Backup from Hetzner

```bash
./backup-download.sh backup_20260102_020000
```

---

## Configuration

All configuration is done via environment variables in `docker-compose.yml`.

### Backup Schedule

```yaml
- BACKUP_SCHEDULE=0 2 * * *  # Daily at 2:00 AM
```

Cron format: `minute hour day month weekday`

Examples:
```bash
0 2 * * *        # Daily at 2:00 AM
0 */6 * * *      # Every 6 hours
0 2 * * 0        # Weekly on Sunday at 2:00 AM
0 3 1 * *        # Monthly on 1st at 3:00 AM
```

### Retention Policy

```yaml
- RETENTION_DAYS=30          # Delete backups older than 30 days
- MAX_BACKUPS=10             # Keep only 10 most recent backups
```

- Set `RETENTION_DAYS=0` to disable age-based cleanup
- Set `MAX_BACKUPS=0` for unlimited backups (not recommended)
- Both policies can be active simultaneously

### Remote Backup

```yaml
- REMOTE_BACKUP_ENABLED=true/false
- HETZNER_USER=your-username
- HETZNER_HOST=your-host.your-storagebox.de
- HETZNER_PORT=23
- HETZNER_REMOTE_PATH=/backups/helpmotivateme
- CLEANUP_REMOTE=true
```

### Timezone

```yaml
- TZ=UTC                     # Or your timezone (e.g., Europe/Copenhagen)
```

---

## Manual Operations

### Create Backup

```bash
# Via helper script (recommended)
./backup-now.sh

# Or directly
docker-compose exec backup /scripts/backup.sh
```

### List Backups

```bash
# Via helper script
./backup-list.sh

# Or directly
docker-compose exec backup ls -lh /backups/local/
```

### Restore Backup

```bash
# Via helper script (recommended)
./backup-restore.sh backup_20260102_143000

# Restore latest
./backup-restore.sh latest

# Or directly
docker-compose exec backup /scripts/restore.sh backup_20260102_143000
```

### Download from Hetzner

```bash
# Via helper script
./backup-download.sh backup_20260102_020000

# Or directly
docker-compose exec backup /scripts/download-backup.sh backup_20260102_020000
```

### Manual Cleanup

```bash
docker-compose exec backup /scripts/cleanup.sh
```

### View Logs

```bash
# Real-time logs
docker-compose logs -f backup

# Last 100 lines
docker-compose logs --tail=100 backup
```

---

## Scheduled Backups

Backups run automatically based on the `BACKUP_SCHEDULE` cron expression.

### Check Schedule

```bash
docker-compose exec backup cat /etc/crontabs/root
```

### View Backup Log

```bash
docker-compose exec backup cat /var/log/backup.log
```

### Change Schedule

1. Update `BACKUP_SCHEDULE` in `docker-compose.yml`
2. Restart backup service:
   ```bash
   docker-compose restart backup
   ```

---

## Troubleshooting

### Backup Service Not Running

```bash
# Check service status
docker-compose ps backup

# View logs
docker-compose logs backup

# Restart service
docker-compose restart backup
```

### Database Connection Failed

Check PostgreSQL credentials in `docker-compose.yml`:
```yaml
- POSTGRES_HOST=postgres
- POSTGRES_PASSWORD=postgres
```

Verify database is accessible:
```bash
docker-compose exec backup pg_isready -h postgres -U postgres
```

### Remote Upload Failed

1. **Check SSH connection:**
   ```bash
   docker-compose exec backup ssh -p 23 u123456@u123456.your-storagebox.de
   ```

2. **Verify SSH key is mounted:**
   ```bash
   docker-compose exec backup ls -la /root/.ssh/
   ```

3. **Check Hetzner credentials:**
   - Verify `HETZNER_USER` and `HETZNER_HOST`
   - Ensure SSH key is authorized on Hetzner

### Restore Failed

1. **Check backup file exists:**
   ```bash
   docker-compose exec backup ls -lh /backups/local/
   ```

2. **Download from remote if needed:**
   ```bash
   ./backup-download.sh backup_name
   ```

3. **Check database connection during restore**

4. **View detailed restore logs**

### Disk Space Issues

1. **Check backup storage:**
   ```bash
   docker-compose exec backup du -sh /backups/
   ```

2. **Adjust retention policy:**
   - Lower `RETENTION_DAYS`
   - Lower `MAX_BACKUPS`

3. **Manual cleanup:**
   ```bash
   docker-compose exec backup /scripts/cleanup.sh
   ```

### Permission Errors

Ensure backup service has read access to volumes:
```yaml
volumes:
  - postgres_data:/var/lib/postgresql/data:ro
  - uploads_data:/uploads:ro
  - backup_data:/backups
```

---

## Best Practices

### Testing

1. **Test backups regularly** - Don't wait for disaster
2. **Test restore process** - Ensure you can actually recover
3. **Verify backup contents** - Check file sizes are reasonable
4. **Monitor backup logs** - Set up alerts for failures

### Security

1. **Protect SSH keys** - Keep private keys secure
2. **Use read-only mounts** - Backup service only needs read access
3. **Encrypt sensitive backups** - Consider encryption for remote storage
4. **Rotate credentials** - Update Hetzner passwords periodically

### Retention

1. **Balance storage vs. history** - More backups = more storage
2. **Consider 3-2-1 rule** - 3 copies, 2 media types, 1 offsite
3. **Test old backups** - Ensure they're still valid

### Monitoring

1. **Check logs daily** - `docker-compose logs backup`
2. **Verify remote uploads** - Confirm backups reach Hetzner
3. **Monitor disk usage** - Ensure you don't run out of space
4. **Set up alerts** - Use monitoring tools for failures

---

## Advanced Usage

### Custom Backup Script

Add custom backup logic to `/backup/scripts/backup.sh`

### Multiple Remote Destinations

Modify backup script to upload to multiple locations

### Backup Encryption

Add GPG encryption to backup script before upload

### Database Point-in-Time Recovery

Use PostgreSQL WAL archiving for more granular recovery

---

## Support

For issues or questions:
1. Check logs: `docker-compose logs backup`
2. Review this documentation
3. Check Hetzner Storage Box documentation
4. Review backup script source code in `/backup/scripts/`

---

## Quick Reference

| Command | Description |
|---------|-------------|
| `./backup-now.sh` | Create backup now |
| `./backup-list.sh` | List all backups |
| `./backup-restore.sh <name>` | Restore specific backup |
| `./backup-restore.sh latest` | Restore latest backup |
| `./backup-download.sh <name>` | Download from Hetzner |
| `docker-compose logs backup` | View logs |
| `docker-compose exec backup /scripts/cleanup.sh` | Manual cleanup |

---

**Remember:** Backups are only useful if you can restore from them. Test your restore process regularly!
