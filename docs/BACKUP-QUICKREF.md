# Backup System - Quick Reference Card

## 🚀 Quick Commands

| Action | Command |
|--------|---------|
| **Create backup now** | `./backup-now.sh` |
| **List backups** | `./backup-list.sh` |
| **Restore latest** | `./backup-restore.sh latest` |
| **Restore specific** | `./backup-restore.sh backup_20260102_143000` |
| **Download from Hetzner** | `./backup-download.sh backup_20260102_143000` |
| **View logs** | `docker-compose logs -f backup` |
| **Manual cleanup** | `docker-compose exec backup /scripts/cleanup.sh` |

## 📋 Configuration (docker-compose.yml)

```yaml
backup:
  environment:
    # Schedule (cron format)
    - BACKUP_SCHEDULE=0 2 * * *         # Daily at 2 AM
    
    # Retention
    - RETENTION_DAYS=30                 # Keep 30 days
    - MAX_BACKUPS=10                    # Keep 10 backups
    
    # Remote Backup
    - REMOTE_BACKUP_ENABLED=false       # true for Hetzner
    - HETZNER_USER=u123456
    - HETZNER_HOST=u123456.your-storagebox.de
```

## 📅 Cron Schedule Examples

| Schedule | Description |
|----------|-------------|
| `0 2 * * *` | Daily at 2:00 AM |
| `0 */6 * * *` | Every 6 hours |
| `0 2 * * 0` | Weekly on Sunday at 2:00 AM |
| `0 3 1 * *` | Monthly on 1st at 3:00 AM |
| `*/30 * * * *` | Every 30 minutes |

## 🔍 Monitoring

```bash
# View real-time logs
docker-compose logs -f backup

# Check last 100 lines
docker-compose logs --tail=100 backup

# View backup log file
docker-compose exec backup cat /var/log/backup.log

# Check cron schedule
docker-compose exec backup cat /etc/crontabs/root
```

## 🧪 Testing Locally

```bash
# 1. Start services
docker-compose up -d

# 2. Create test data (use your app)

# 3. Create backup
./backup-now.sh

# 4. Verify backup
./backup-list.sh

# 5. Modify data

# 6. Restore
./backup-restore.sh latest

# 7. Verify data restored
```

## 🌩️ Hetzner Setup (Production)

```bash
# 1. Generate SSH key
ssh-keygen -t rsa -b 4096 -f ~/.ssh/hetzner_backup

# 2. Add to Hetzner
cat ~/.ssh/hetzner_backup.pub | ssh -p 23 u123456@u123456.your-storagebox.de "cat >> .ssh/authorized_keys"

# 3. Test connection
ssh -p 23 -i ~/.ssh/hetzner_backup u123456@u123456.your-storagebox.de

# 4. Update docker-compose.yml
# Set REMOTE_BACKUP_ENABLED=true
# Add Hetzner credentials
# Mount SSH key

# 5. Deploy
docker-compose up -d backup
```

## 📦 Backup Contents

Each backup includes:
- `database.dump` - PostgreSQL database (compressed)
- `uploads.tar.gz` - All uploaded files
- `metadata.txt` - Backup information

## 🔧 Troubleshooting

| Problem | Solution |
|---------|----------|
| Service not starting | `docker-compose logs backup` |
| Database connection failed | Check POSTGRES_* env vars |
| Remote upload failed | Verify SSH key and credentials |
| Restore failed | Check backup exists with `./backup-list.sh` |
| Disk space issues | Lower retention settings |

## 📚 Documentation

- **BACKUP.md** - Complete documentation
- **BACKUP-TEST.md** - Testing guide
- **BACKUP-SUMMARY.md** - Implementation overview
- **backup.env.example** - Configuration template

## ⚙️ Service Management

```bash
# Start backup service
docker-compose up -d backup

# Restart service
docker-compose restart backup

# Stop service
docker-compose stop backup

# View service status
docker-compose ps backup

# Rebuild service
docker-compose up -d --build backup
```

## 💾 Volume Management

```bash
# List volumes
docker volume ls | grep backup

# Inspect backup volume
docker volume inspect helpmotivateme_backup_data

# Backup volume to host (emergency)
docker run --rm -v helpmotivateme_backup_data:/data -v $(pwd):/backup alpine tar czf /backup/emergency-backup.tar.gz /data
```

## 🚨 Emergency Procedures

### Quick Backup Before Major Change
```bash
./backup-now.sh
```

### Immediate Restore After Problem
```bash
./backup-restore.sh latest
docker-compose restart backend
```

### Recover Deleted Backup
```bash
# If uploaded to Hetzner
./backup-download.sh backup_20260102_143000
```

## 🎯 Best Practices

✅ Test backups regularly  
✅ Test restore process monthly  
✅ Monitor backup logs weekly  
✅ Keep SSH keys secure  
✅ Verify remote uploads  
✅ Document restore procedures  
✅ Set up alerts for failures  

## 📞 Quick Help

```bash
# All helper scripts show usage without arguments
./backup-restore.sh          # Shows available backups
./backup-download.sh         # Shows usage
```

---

**Remember**: Backups are only useful if you test them! 🧪
