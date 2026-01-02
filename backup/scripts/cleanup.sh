#!/bin/bash
set -e

# Load configuration
source /scripts/config.sh

LOCAL_BACKUP_DIR="/backups/local"

echo "==================================================================="
echo "Running backup cleanup..."
echo "==================================================================="
echo "Date: $(date)"
echo "-------------------------------------------------------------------"

if [ ! -d "${LOCAL_BACKUP_DIR}" ]; then
    echo "ℹ️  No backup directory found, nothing to clean"
    exit 0
fi

# Count existing backups
TOTAL_BACKUPS=$(ls -1 "${LOCAL_BACKUP_DIR}"/backup_*.tar.gz 2>/dev/null | wc -l)
echo "ℹ️  Total backups found: ${TOTAL_BACKUPS}"

if [ ${TOTAL_BACKUPS} -eq 0 ]; then
    echo "ℹ️  No backups to clean"
    exit 0
fi

DELETED_COUNT=0

# =================================================================
# Cleanup by Age (RETENTION_DAYS)
# =================================================================
if [ "${RETENTION_DAYS}" -gt 0 ]; then
    echo ""
    echo "🧹 Removing backups older than ${RETENTION_DAYS} days..."
    echo "-------------------------------------------------------------------"
    
    # Find and delete old backups
    OLD_BACKUPS=$(find "${LOCAL_BACKUP_DIR}" -name "backup_*.tar.gz" -type f -mtime +${RETENTION_DAYS} 2>/dev/null)
    
    if [ -n "${OLD_BACKUPS}" ]; then
        while IFS= read -r backup; do
            if [ -f "${backup}" ]; then
                BACKUP_NAME=$(basename "${backup}")
                BACKUP_SIZE=$(du -h "${backup}" | cut -f1)
                echo "  Deleting: ${BACKUP_NAME} (${BACKUP_SIZE})"
                rm -f "${backup}"
                DELETED_COUNT=$((DELETED_COUNT + 1))
            fi
        done <<< "${OLD_BACKUPS}"
        
        echo "✅ Deleted ${DELETED_COUNT} old backup(s)"
    else
        echo "ℹ️  No backups older than ${RETENTION_DAYS} days"
    fi
fi

# =================================================================
# Cleanup by Count (MAX_BACKUPS)
# =================================================================
if [ "${MAX_BACKUPS}" -gt 0 ]; then
    # Recount after age-based cleanup
    CURRENT_BACKUPS=$(ls -1 "${LOCAL_BACKUP_DIR}"/backup_*.tar.gz 2>/dev/null | wc -l)
    
    if [ ${CURRENT_BACKUPS} -gt ${MAX_BACKUPS} ]; then
        echo ""
        echo "🧹 Keeping only ${MAX_BACKUPS} most recent backups..."
        echo "-------------------------------------------------------------------"
        
        EXCESS_COUNT=$((CURRENT_BACKUPS - MAX_BACKUPS))
        
        # Delete oldest backups beyond MAX_BACKUPS
        ls -t "${LOCAL_BACKUP_DIR}"/backup_*.tar.gz | tail -n ${EXCESS_COUNT} | while read -r backup; do
            if [ -f "${backup}" ]; then
                BACKUP_NAME=$(basename "${backup}")
                BACKUP_SIZE=$(du -h "${backup}" | cut -f1)
                echo "  Deleting: ${BACKUP_NAME} (${BACKUP_SIZE})"
                rm -f "${backup}"
                DELETED_COUNT=$((DELETED_COUNT + 1))
            fi
        done
        
        echo "✅ Deleted ${EXCESS_COUNT} excess backup(s)"
    else
        echo ""
        echo "ℹ️  Backup count (${CURRENT_BACKUPS}) within limit (${MAX_BACKUPS})"
    fi
fi

# =================================================================
# Cleanup Remote Backups (if enabled)
# =================================================================
if [ "${REMOTE_BACKUP_ENABLED}" = "true" ] && [ "${CLEANUP_REMOTE}" = "true" ]; then
    echo ""
    echo "☁️  Cleaning up remote backups..."
    echo "-------------------------------------------------------------------"
    
    if [ -n "${HETZNER_USER}" ] && [ -n "${HETZNER_HOST}" ]; then
        # Get list of remote backups
        REMOTE_BACKUPS=$(ssh -p "${HETZNER_PORT}" \
            -o StrictHostKeyChecking=no \
            -o UserKnownHostsFile=/dev/null \
            "${HETZNER_USER}@${HETZNER_HOST}" \
            "ls -t ${HETZNER_REMOTE_PATH}/backup_*.tar.gz 2>/dev/null" 2>/dev/null || echo "")
        
        if [ -n "${REMOTE_BACKUPS}" ]; then
            REMOTE_COUNT=$(echo "${REMOTE_BACKUPS}" | wc -l)
            echo "ℹ️  Remote backups found: ${REMOTE_COUNT}"
            
            # Apply same retention policy to remote
            if [ ${REMOTE_COUNT} -gt ${MAX_BACKUPS} ]; then
                REMOTE_EXCESS=$((REMOTE_COUNT - MAX_BACKUPS))
                
                echo "${REMOTE_BACKUPS}" | tail -n ${REMOTE_EXCESS} | while read -r remote_backup; do
                    REMOTE_NAME=$(basename "${remote_backup}")
                    echo "  Deleting remote: ${REMOTE_NAME}"
                    
                    ssh -p "${HETZNER_PORT}" \
                        -o StrictHostKeyChecking=no \
                        -o UserKnownHostsFile=/dev/null \
                        "${HETZNER_USER}@${HETZNER_HOST}" \
                        "rm -f ${HETZNER_REMOTE_PATH}/${REMOTE_NAME}" 2>/dev/null || true
                done
                
                echo "✅ Cleaned up remote backups"
            else
                echo "ℹ️  Remote backup count within limit"
            fi
        else
            echo "ℹ️  No remote backups found"
        fi
    else
        echo "⚠️  Remote storage not configured"
    fi
fi

# =================================================================
# Summary
# =================================================================
REMAINING_BACKUPS=$(ls -1 "${LOCAL_BACKUP_DIR}"/backup_*.tar.gz 2>/dev/null | wc -l)
TOTAL_SIZE=$(du -sh "${LOCAL_BACKUP_DIR}" 2>/dev/null | cut -f1)

echo ""
echo "==================================================================="
echo "✅ Cleanup completed"
echo "==================================================================="
echo "Backups deleted: ${DELETED_COUNT}"
echo "Backups remaining: ${REMAINING_BACKUPS}"
echo "Total size: ${TOTAL_SIZE}"
echo "==================================================================="
echo ""
