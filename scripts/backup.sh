#!/usr/bin/env bash
set -euo pipefail
cd /opt/aerarium
set -a
. ./.env
set +a
STAMP=$(date +%F-%H%M)
docker compose -f docker-compose.prod.yml exec -T postgres \
  pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" | gzip | \
  aws s3 cp - "s3://aerarium-db-backups/aerarium-${STAMP}.sql.gz"
echo "$(date -Iseconds) backup ok: aerarium-${STAMP}.sql.gz"
