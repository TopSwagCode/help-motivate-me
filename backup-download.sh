#!/bin/bash

# Helper script to download a backup from Hetzner Storage Box

BACKUP_NAME="$1"

if [ -z "${BACKUP_NAME}" ]; then
    echo "Usage: $0 <backup_name>"
    echo "Example: $0 backup_20260102_143000"
    exit 1
fi

echo "==================================================================="
echo "Downloading backup from Hetzner Storage Box"
echo "==================================================================="
echo "Backup: ${BACKUP_NAME}"
echo ""

docker-compose exec backup /scripts/download-backup.sh "${BACKUP_NAME}"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Download complete!"
    echo ""
    echo "To restore this backup:"
    echo "  ./backup-restore.sh ${BACKUP_NAME}"
fi
