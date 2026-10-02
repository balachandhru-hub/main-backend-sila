#!/usr/bin/env bash
# Per-boot runtime initialization: bring up PostgreSQL and ensure the dev
# role/database exist. Idempotent and safe across restarts.
set -euo pipefail

# Start the PostgreSQL 16 cluster if it is not already accepting connections.
if ! pg_isready -h 127.0.0.1 -p 5432 >/dev/null 2>&1; then
  sudo pg_ctlcluster 16 main start
fi

# Wait for readiness.
for _ in $(seq 1 30); do
  if pg_isready -h 127.0.0.1 -p 5432 >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

# Ensure the development role and database exist (matches docker-compose.yml).
sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='sila'" | grep -q 1 \
  || sudo -u postgres psql -c "CREATE ROLE sila WITH LOGIN PASSWORD 'sila';"
sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='sila_me'" | grep -q 1 \
  || sudo -u postgres createdb -O sila sila_me

echo "PostgreSQL ready on 127.0.0.1:5432 (database: sila_me, role: sila)"
