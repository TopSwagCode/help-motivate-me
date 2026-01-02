#!/bin/bash
set -e

# Load configuration
source /scripts/config.sh

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="backup_${TIMESTAMP}"
BACKUP_DIR="/backups/${BACKUP_NAME}"
LOCAL_BACKUP_DIR="/backups/local"

echo "==================================================================="
echo "Starting backup: ${BACKUP_NAME}"
echo "==================================================================="
echo "Date: $(date)"
echo "-------------------------------------------------------------------"

# Create backup directories
mkdir -p "${BACKUP_DIR}"
mkdir -p "${LOCAL_BACKUP_DIR}"

# =================================================================
# Backup PostgreSQL Database
# =================================================================
echo ""
echo "📦 Backing up PostgreSQL database..."
echo "-------------------------------------------------------------------"

PGPASSWORD="${POSTGRES_PASSWORD}" pg_dump \
    -h "${POSTGRES_HOST}" \
    -p "${POSTGRES_PORT}" \
    -U "${POSTGRES_USER}" \
    -d "${POSTGRES_DB}" \
    -F c \
    -f "${BACKUP_DIR}/database.dump"

if [ $? -eq 0 ]; then
    DB_SIZE=$(du -h "${BACKUP_DIR}/database.dump" | cut -f1)
    echo "✅ Database backup completed: ${DB_SIZE}"
else
    echo "❌ Database backup failed!"
    exit 1
fi

# =================================================================
# Backup Uploads Directory
# =================================================================
echo ""
echo "📦 Backing up uploads directory..."
echo "-------------------------------------------------------------------"

if [ -d "/uploads" ]; then
    tar -czf "${BACKUP_DIR}/uploads.tar.gz" -C /uploads .
    
    if [ $? -eq 0 ]; then
        UPLOADS_SIZE=$(du -h "${BACKUP_DIR}/uploads.tar.gz" | cut -f1)
        echo "✅ Uploads backup completed: ${UPLOADS_SIZE}"
    else
        echo "❌ Uploads backup failed!"
        exit 1
    fi
else
    echo "⚠️  Uploads directory not found, skipping..."
fi

# =================================================================
# Create Metadata File
# =================================================================
echo ""
echo "📝 Creating backup metadata..."
echo "-------------------------------------------------------------------"

cat > "${BACKUP_DIR}/metadata.txt" <<EOF
Backup Name: ${BACKUP_NAME}
Timestamp: ${TIMESTAMP}
Date: $(date)
Hostname: $(hostname)
Database: ${POSTGRES_DB}
Files Included:
  - database.dump (PostgreSQL backup)
  - uploads.tar.gz (Uploaded files)
EOF

echo "✅ Metadata created"

# =================================================================
# Create Combined Archive
# =================================================================
echo ""
echo "📦 Creating combined backup archive..."
echo "-------------------------------------------------------------------"

cd /backups
tar -czf "${BACKUP_NAME}.tar.gz" "${BACKUP_NAME}/"

if [ $? -eq 0 ]; then
    TOTAL_SIZE=$(du -h "${BACKUP_NAME}.tar.gz" | cut -f1)
    echo "✅ Combined archive created: ${TOTAL_SIZE}"
    
    # Move to local backup directory
    mv "${BACKUP_NAME}.tar.gz" "${LOCAL_BACKUP_DIR}/"
    
    # Clean up temporary directory
    rm -rf "${BACKUP_DIR}"
else
    echo "❌ Failed to create combined archive!"
    exit 1
fi

# =================================================================
# Upload to Remote Storage (Hetzner Storage Box)
# =================================================================
if [ "${REMOTE_BACKUP_ENABLED}" = "true" ]; then
    echo ""
    echo "☁️  Uploading to remote storage..."
    echo "-------------------------------------------------------------------"
    
    if [ -n "${HETZNER_USER}" ] && [ -n "${HETZNER_HOST}" ]; then
        # Ensure remote directory exists
        ssh -p "${HETZNER_PORT}" \
            -o StrictHostKeyChecking=no \
            -o UserKnownHostsFile=/dev/null \
            "${HETZNER_USER}@${HETZNER_HOST}" \
            "mkdir -p ${HETZNER_REMOTE_PATH}" 2>/dev/null || true
        
        # Upload via rsync
        rsync -avz -e "ssh -p ${HETZNER_PORT} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null" \
            "${LOCAL_BACKUP_DIR}/${BACKUP_NAME}.tar.gz" \
            "${HETZNER_USER}@${HETZNER_HOST}:${HETZNER_REMOTE_PATH}/"
        
        if [ $? -eq 0 ]; then
            echo "✅ Uploaded to remote storage"
        else
            echo "⚠️  Remote upload failed (backup saved locally)"
        fi
    else
        echo "⚠️  Remote storage not configured, skipping upload"
    fi
else
    echo ""
    echo "ℹ️  Remote backup disabled, backup saved locally only"
fi

# =================================================================
# Cleanup Old Backups
# =================================================================
echo ""
echo "🧹 Running cleanup..."
echo "-------------------------------------------------------------------"

/scripts/cleanup.sh

# =================================================================
# Summary
# =================================================================
echo ""
echo "==================================================================="
echo "✅ Backup completed successfully!"
echo "==================================================================="
echo "Backup name: ${BACKUP_NAME}.tar.gz"
echo "Location: ${LOCAL_BACKUP_DIR}/${BACKUP_NAME}.tar.gz"
echo "Total size: ${TOTAL_SIZE}"
echo "Timestamp: $(date)"
echo "==================================================================="
echo ""
