#!/bin/bash
set -e

# Load configuration
source /scripts/config.sh

BACKUP_NAME="$1"

if [ -z "${BACKUP_NAME}" ]; then
    echo "Usage: $0 <backup_name>"
    echo "Example: $0 backup_20260102_143000"
    exit 1
fi

echo "==================================================================="
echo "Downloading backup from remote storage"
echo "==================================================================="
echo "Backup: ${BACKUP_NAME}"
echo "-------------------------------------------------------------------"

if [ "${REMOTE_BACKUP_ENABLED}" != "true" ]; then
    echo "❌ Remote backup is not enabled!"
    exit 1
fi

if [ -z "${HETZNER_USER}" ] || [ -z "${HETZNER_HOST}" ]; then
    echo "❌ Hetzner credentials not configured!"
    exit 1
fi

LOCAL_BACKUP_DIR="/backups/local"
mkdir -p "${LOCAL_BACKUP_DIR}"

REMOTE_FILE="${HETZNER_REMOTE_PATH}/${BACKUP_NAME}.tar.gz"
LOCAL_FILE="${LOCAL_BACKUP_DIR}/${BACKUP_NAME}.tar.gz"

# Check if backup exists remotely
echo "ℹ️  Checking remote backup..."
ssh -p "${HETZNER_PORT}" \
    -o StrictHostKeyChecking=no \
    -o UserKnownHostsFile=/dev/null \
    "${HETZNER_USER}@${HETZNER_HOST}" \
    "test -f ${REMOTE_FILE}"

if [ $? -ne 0 ]; then
    echo "❌ Backup not found on remote storage: ${BACKUP_NAME}"
    echo ""
    echo "Available remote backups:"
    ssh -p "${HETZNER_PORT}" \
        -o StrictHostKeyChecking=no \
        -o UserKnownHostsFile=/dev/null \
        "${HETZNER_USER}@${HETZNER_HOST}" \
        "ls -lh ${HETZNER_REMOTE_PATH}/" 2>/dev/null | grep "backup_" || echo "None found"
    exit 1
fi

# Download backup
echo "📥 Downloading..."
rsync -avz --progress \
    -e "ssh -p ${HETZNER_PORT} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null" \
    "${HETZNER_USER}@${HETZNER_HOST}:${REMOTE_FILE}" \
    "${LOCAL_FILE}"

if [ $? -eq 0 ]; then
    FILE_SIZE=$(du -h "${LOCAL_FILE}" | cut -f1)
    echo ""
    echo "✅ Download completed: ${FILE_SIZE}"
    echo "ℹ️  Backup saved to: ${LOCAL_FILE}"
    echo ""
    echo "To restore this backup:"
    echo "  docker-compose exec backup /scripts/restore.sh ${BACKUP_NAME}"
else
    echo "❌ Download failed!"
    exit 1
fi
