#!/bin/bash

# Helper script to list available backups

echo "==================================================================="
echo "Available Backups"
echo "==================================================================="
echo ""

docker-compose exec backup sh -c 'ls -lh /backups/local/*.tar.gz 2>/dev/null | awk "{print \$9, \"(\"\$5\")\"}" | sed "s|/backups/local/||"'

if [ $? -ne 0 ]; then
    echo "No backups found or backup service not running."
    echo ""
    echo "To create a backup:"
    echo "  ./backup-now.sh"
fi

echo ""
echo "==================================================================="
echo ""
echo "To restore a backup:"
echo "  ./backup-restore.sh <backup_name>"
echo ""
echo "To restore latest backup:"
echo "  ./backup-restore.sh latest"
echo "==================================================================="
