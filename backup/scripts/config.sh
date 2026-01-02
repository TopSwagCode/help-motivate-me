#!/bin/bash

# =================================================================
# Backup Configuration
# =================================================================

# PostgreSQL Configuration
export POSTGRES_HOST="${POSTGRES_HOST:-postgres}"
export POSTGRES_PORT="${POSTGRES_PORT:-5432}"
export POSTGRES_DB="${POSTGRES_DB:-helpmotivateme}"
export POSTGRES_USER="${POSTGRES_USER:-postgres}"
export POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-postgres}"

# Retention Policy
export RETENTION_DAYS="${RETENTION_DAYS:-30}"        # Delete backups older than X days (0 = disabled)
export MAX_BACKUPS="${MAX_BACKUPS:-10}"              # Keep only X most recent backups (0 = unlimited)

# Remote Backup (Hetzner Storage Box)
export REMOTE_BACKUP_ENABLED="${REMOTE_BACKUP_ENABLED:-false}"
export HETZNER_USER="${HETZNER_USER:-}"
export HETZNER_HOST="${HETZNER_HOST:-}"
export HETZNER_PORT="${HETZNER_PORT:-23}"
export HETZNER_REMOTE_PATH="${HETZNER_REMOTE_PATH:-/backups/helpmotivateme}"
export CLEANUP_REMOTE="${CLEANUP_REMOTE:-true}"      # Apply retention policy to remote backups

# Backup Schedule (cron format)
# Default: Every day at 2:00 AM
export BACKUP_SCHEDULE="${BACKUP_SCHEDULE:-0 2 * * *}"

# Timezone
export TZ="${TZ:-UTC}"

# =================================================================
# Display Configuration (for debugging)
# =================================================================
if [ "${SHOW_CONFIG}" = "true" ]; then
    echo "==================================================================="
    echo "Backup Configuration"
    echo "==================================================================="
    echo "PostgreSQL:"
    echo "  Host: ${POSTGRES_HOST}"
    echo "  Port: ${POSTGRES_PORT}"
    echo "  Database: ${POSTGRES_DB}"
    echo "  User: ${POSTGRES_USER}"
    echo ""
    echo "Retention:"
    echo "  Days: ${RETENTION_DAYS}"
    echo "  Max Backups: ${MAX_BACKUPS}"
    echo ""
    echo "Remote Backup:"
    echo "  Enabled: ${REMOTE_BACKUP_ENABLED}"
    echo "  Host: ${HETZNER_HOST}"
    echo "  User: ${HETZNER_USER}"
    echo "  Port: ${HETZNER_PORT}"
    echo "  Path: ${HETZNER_REMOTE_PATH}"
    echo "  Cleanup Remote: ${CLEANUP_REMOTE}"
    echo ""
    echo "Schedule:"
    echo "  Cron: ${BACKUP_SCHEDULE}"
    echo "  Timezone: ${TZ}"
    echo "==================================================================="
fi
