#!/bin/bash

# Helper script to trigger a manual backup

echo "==================================================================="
echo "Triggering manual backup..."
echo "==================================================================="

docker-compose exec backup /scripts/backup.sh

echo ""
echo "==================================================================="
echo "ℹ️  Backup complete!"
echo "==================================================================="
echo ""
echo "To list backups:"
echo "  ./backup-list.sh"
echo ""
echo "To restore a backup:"
echo "  ./backup-restore.sh <backup_name>"
echo "==================================================================="
