#!/bin/bash

# Helper script to restore from a backup

BACKUP_NAME="$1"

if [ -z "${BACKUP_NAME}" ]; then
    echo "==================================================================="
    echo "Available Backups"
    echo "==================================================================="
    
    docker-compose exec backup /scripts/restore.sh
    
    echo ""
    echo "Usage: $0 <backup_name>"
    echo "Example: $0 backup_20260102_143000"
    echo ""
    echo "Or restore latest:"
    echo "  $0 latest"
    echo "==================================================================="
    exit 1
fi

echo "==================================================================="
echo "Restoring from backup: ${BACKUP_NAME}"
echo "==================================================================="
echo ""
echo "⚠️  WARNING: This will overwrite your current database and files!"
echo ""
read -p "Are you sure you want to continue? (yes/no): " -r
echo

if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo "Restore cancelled."
    exit 0
fi

# Execute restore (skip confirmation prompt since we already asked)
docker-compose exec -T -e SKIP_CONFIRM=true backup /scripts/restore.sh "${BACKUP_NAME}"

if [ $? -eq 0 ]; then
    echo ""
    echo "==================================================================="
    echo "Restarting backend service..."
    echo "==================================================================="
    
    docker-compose restart backend
    
    echo ""
    echo "✅ Restore completed and backend restarted!"
else
    echo ""
    echo "❌ Restore failed!"
    exit 1
fi
