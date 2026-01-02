#!/bin/bash
set -e

# Load configuration
source /scripts/config.sh

LOCAL_BACKUP_DIR="/backups/local"

# =================================================================
# Parse Arguments
# =================================================================
SKIP_CONFIRM="${SKIP_CONFIRM:-false}"

if [ -z "$1" ]; then
    echo "==================================================================="
    echo "Available backups:"
    echo "==================================================================="
    
    if [ -d "${LOCAL_BACKUP_DIR}" ]; then
        ls -lh "${LOCAL_BACKUP_DIR}/" | grep "backup_" | awk '{print $9, "(" $5 ")"}'
    fi
    
    echo ""
    echo "Usage: $0 <backup_name>"
    echo "Example: $0 backup_20260102_143000"
    echo ""
    echo "Or restore latest:"
    echo "  $0 latest"
    echo "==================================================================="
    exit 1
fi

BACKUP_NAME="$1"

# Handle 'latest' keyword
if [ "${BACKUP_NAME}" = "latest" ]; then
    BACKUP_NAME=$(ls -t "${LOCAL_BACKUP_DIR}"/backup_*.tar.gz 2>/dev/null | head -1 | xargs basename | sed 's/.tar.gz//')
    
    if [ -z "${BACKUP_NAME}" ]; then
        echo "❌ No backups found!"
        exit 1
    fi
    
    echo "ℹ️  Using latest backup: ${BACKUP_NAME}"
fi

BACKUP_FILE="${LOCAL_BACKUP_DIR}/${BACKUP_NAME}.tar.gz"

if [ ! -f "${BACKUP_FILE}" ]; then
    echo "❌ Backup file not found: ${BACKUP_FILE}"
    echo ""
    echo "Try downloading from remote storage first:"
    echo "  /scripts/download-backup.sh ${BACKUP_NAME}"
    exit 1
fi

echo "==================================================================="
echo "Starting restore from: ${BACKUP_NAME}"
echo "==================================================================="
echo "Date: $(date)"
echo "⚠️  WARNING: This will overwrite existing data!"
echo "-------------------------------------------------------------------"

# Ask for confirmation (unless SKIP_CONFIRM is set)
if [ "${SKIP_CONFIRM}" != "true" ]; then
    read -p "Continue with restore? (yes/no): " -r
    echo
    if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        echo "Restore cancelled."
        exit 0
    fi
else
    echo "ℹ️  Confirmation skipped (SKIP_CONFIRM=true)"
    echo ""
fi

# =================================================================
# Extract Backup Archive
# =================================================================
echo ""
echo "📦 Extracting backup archive..."
echo "-------------------------------------------------------------------"

TEMP_DIR="/tmp/restore_${BACKUP_NAME}"
rm -rf "${TEMP_DIR}"
mkdir -p "${TEMP_DIR}"

tar -xzf "${BACKUP_FILE}" -C "${TEMP_DIR}"

if [ $? -eq 0 ]; then
    echo "✅ Archive extracted"
else
    echo "❌ Failed to extract archive!"
    exit 1
fi

# Find the backup directory (it should be the only directory in TEMP_DIR)
BACKUP_DIR=$(find "${TEMP_DIR}" -mindepth 1 -maxdepth 1 -type d | head -1)

if [ ! -d "${BACKUP_DIR}" ]; then
    echo "❌ Backup directory not found in archive!"
    exit 1
fi

# =================================================================
# Restore PostgreSQL Database
# =================================================================
echo ""
echo "🗄️  Restoring PostgreSQL database..."
echo "-------------------------------------------------------------------"

if [ -f "${BACKUP_DIR}/database.dump" ]; then
    # Drop existing database connections
    PGPASSWORD="${POSTGRES_PASSWORD}" psql \
        -h "${POSTGRES_HOST}" \
        -p "${POSTGRES_PORT}" \
        -U "${POSTGRES_USER}" \
        -d postgres \
        -c "SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '${POSTGRES_DB}' AND pid <> pg_backend_pid();" \
        2>/dev/null || true
    
    # Drop and recreate database
    PGPASSWORD="${POSTGRES_PASSWORD}" psql \
        -h "${POSTGRES_HOST}" \
        -p "${POSTGRES_PORT}" \
        -U "${POSTGRES_USER}" \
        -d postgres \
        -c "DROP DATABASE IF EXISTS ${POSTGRES_DB};"
    
    PGPASSWORD="${POSTGRES_PASSWORD}" psql \
        -h "${POSTGRES_HOST}" \
        -p "${POSTGRES_PORT}" \
        -U "${POSTGRES_USER}" \
        -d postgres \
        -c "CREATE DATABASE ${POSTGRES_DB};"
    
    # Restore database
    PGPASSWORD="${POSTGRES_PASSWORD}" pg_restore \
        -h "${POSTGRES_HOST}" \
        -p "${POSTGRES_PORT}" \
        -U "${POSTGRES_USER}" \
        -d "${POSTGRES_DB}" \
        -v \
        "${BACKUP_DIR}/database.dump"
    
    if [ $? -eq 0 ]; then
        echo "✅ Database restored successfully"
    else
        echo "❌ Database restore failed!"
        exit 1
    fi
else
    echo "⚠️  Database backup not found, skipping..."
fi

# =================================================================
# Restore Uploads Directory
# =================================================================
echo ""
echo "📁 Restoring uploads directory..."
echo "-------------------------------------------------------------------"

if [ -f "${BACKUP_DIR}/uploads.tar.gz" ]; then
    # Clear existing uploads directory
    if [ -d "/uploads" ] && [ "$(ls -A /uploads 2>/dev/null)" ]; then
        echo "ℹ️  Clearing existing uploads..."
        rm -rf /uploads/*
    fi
    
    # Create uploads directory if it doesn't exist
    mkdir -p /uploads
    
    # Extract uploads
    tar -xzf "${BACKUP_DIR}/uploads.tar.gz" -C /uploads
    
    if [ $? -eq 0 ]; then
        UPLOADS_COUNT=$(find /uploads -type f | wc -l)
        echo "✅ Uploads restored: ${UPLOADS_COUNT} files"
    else
        echo "❌ Uploads restore failed!"
        exit 1
    fi
else
    echo "⚠️  Uploads backup not found, skipping..."
fi

# =================================================================
# Cleanup
# =================================================================
echo ""
echo "🧹 Cleaning up temporary files..."
echo "-------------------------------------------------------------------"

rm -rf "${TEMP_DIR}"
echo "✅ Cleanup completed"

# =================================================================
# Summary
# =================================================================
echo ""
echo "==================================================================="
echo "✅ Restore completed successfully!"
echo "==================================================================="
echo "Restored from: ${BACKUP_NAME}"
echo "Timestamp: $(date)"
echo ""
echo "ℹ️  You may need to restart your application services:"
echo "   docker-compose restart backend"
echo "==================================================================="
echo ""
