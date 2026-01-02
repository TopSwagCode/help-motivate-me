# Backup System - Quick Test Guide

This guide will help you test the backup and restore system locally.

## Prerequisites

- Docker and Docker Compose installed
- Application running with some test data

## Step-by-Step Test

### 1. Start the Services

```bash
cd /Users/topswagcode/git/help-motivate-me
docker-compose up -d
```

Wait for all services to start:
```bash
docker-compose ps
```

All services should show "Up" status.

### 2. Create Test Data

1. Open http://localhost:5173
2. Create a user account
3. Add some journal entries
4. Upload some images
5. Note down specific data you created (for verification later)

### 3. Create First Backup

```bash
./backup-now.sh
```

Expected output:
```
===================================================================
Starting backup: backup_20260102_HHMMSS
===================================================================
...
📦 Backing up PostgreSQL database...
✅ Database backup completed: 125K
...
📦 Backing up uploads directory...
✅ Uploads backup completed: 2.3M
...
✅ Backup completed successfully!
===================================================================
```

### 4. Verify Backup Was Created

```bash
./backup-list.sh
```

You should see your backup listed with its size.

### 5. Create More Test Data

1. Add more journal entries
2. Upload more images
3. Modify existing entries
4. Note what you changed

### 6. Test Restore

Restore to the first backup (before the new changes):

```bash
./backup-restore.sh latest
```

When prompted, type `yes` and press Enter.

Expected output:
```
===================================================================
Starting restore from: backup_20260102_HHMMSS
===================================================================
⚠️  WARNING: This will overwrite existing data!
Continue with restore? (yes/no): yes
...
🗄️  Restoring PostgreSQL database...
✅ Database restored successfully
...
📁 Restoring uploads directory...
✅ Uploads restored: X files
...
✅ Restore completed successfully!
===================================================================
```

### 7. Verify Restoration

1. Refresh your browser
2. Verify your data is back to the state of the first backup
3. The new data you added in step 5 should be gone
4. The original test data from step 2 should be present

### 8. Test Multiple Backups

Create several backups:

```bash
# Create backup 1
./backup-now.sh

# Make changes
# Add/modify data

# Create backup 2
./backup-now.sh

# Make more changes

# Create backup 3
./backup-now.sh
```

List all backups:
```bash
./backup-list.sh
```

Restore to a specific backup:
```bash
./backup-restore.sh backup_20260102_143000
```

### 9. Test Retention Policy

The default configuration keeps:
- Backups for 30 days
- Maximum 10 backups

To test retention quickly, update docker-compose.yml:

```yaml
backup:
  environment:
    - MAX_BACKUPS=3  # Keep only 3 backups
```

Restart backup service:
```bash
docker-compose restart backup
```

Create more than 3 backups and verify old ones are deleted automatically.

### 10. View Backup Contents (Optional)

To inspect what's inside a backup:

```bash
# Copy backup from container
docker cp helpmotivateme-backup:/backups/local/backup_20260102_143000.tar.gz .

# Extract it
tar -xzf backup_20260102_143000.tar.gz

# View contents
cd backup_20260102_143000/
ls -lh

# You should see:
# - database.dump
# - uploads.tar.gz
# - metadata.txt

# View metadata
cat metadata.txt
```

## Test Checklist

- [ ] Backup service starts successfully
- [ ] Can create manual backup
- [ ] Backup contains database dump
- [ ] Backup contains uploaded files
- [ ] Can list backups
- [ ] Can restore from specific backup
- [ ] Data is correctly restored
- [ ] Can restore latest backup
- [ ] Multiple backups work
- [ ] Old backups are cleaned up based on retention policy
- [ ] Backend restarts after restore

## Common Issues

### Backup Service Not Starting

```bash
docker-compose logs backup
```

Check for error messages.

### Permission Errors

Ensure volumes have correct permissions:
```bash
docker-compose down -v
docker-compose up -d
```

### Database Connection Failed

Verify PostgreSQL is running:
```bash
docker-compose ps postgres
docker-compose logs postgres
```

### Restore Doesn't Work

1. Check backup file exists:
   ```bash
   ./backup-list.sh
   ```

2. Try restoring with full backup name:
   ```bash
   ./backup-restore.sh backup_20260102_143000
   ```

3. Check logs:
   ```bash
   docker-compose logs backup
   ```

## Next Steps

Once local testing is successful:

1. Review BACKUP.md for production setup
2. Configure Hetzner Storage Box credentials
3. Test remote backup upload
4. Test downloading and restoring from remote backup
5. Set up monitoring for backup failures

## Clean Up

To remove all test backups:

```bash
docker-compose exec backup sh -c 'rm -rf /backups/local/*'
```

To start fresh:
```bash
docker-compose down -v
docker-compose up -d
```

This will delete all data and volumes.
