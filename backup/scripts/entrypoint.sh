#!/bin/bash

# Load configuration
source /scripts/config.sh

# Setup cron job based on BACKUP_SCHEDULE
echo "${BACKUP_SCHEDULE} /scripts/backup.sh >> /var/log/backup.log 2>&1" > /etc/crontabs/root

echo "==================================================================="
echo "Backup Service Started"
echo "==================================================================="
echo "Date: $(date)"
echo "Timezone: ${TZ}"
echo "Schedule: ${BACKUP_SCHEDULE}"
echo "==================================================================="
echo ""

# Display configuration
SHOW_CONFIG=true source /scripts/config.sh

echo ""
echo "ℹ️  Cron job configured. Waiting for scheduled backups..."
echo "ℹ️  To trigger manual backup: docker-compose exec backup /scripts/backup.sh"
echo "ℹ️  To restore: docker-compose exec backup /scripts/restore.sh <backup_name>"
echo "==================================================================="
echo ""

# Start cron in foreground
# Note: "setpgid: Operation not permitted" warning is harmless on macOS/Docker
crond -f -l 2
