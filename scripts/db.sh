#!/usr/bin/env bash
# Maintain the SILA ME local PostgreSQL database on Mac (Docker or native Postgres).
# Does not print passwords, connection strings, or password hashes.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${ROOT}/docker-compose.yml"
DUMPS_DIR="${ROOT}/dumps"
LOCAL_DUMPS_DIR="${DUMPS_DIR}/local"
SNAPSHOTS_DIR="${DUMPS_DIR}/snapshots"
DEFAULT_DUMP="${LOCAL_DUMPS_DIR}/sila-me.dump"
SNAPSHOT_DATABASES=(sila_platform sila_me sila_five_test sila_five_prod)

load_env_file() {
  local file="$1"
  [[ -f "$file" ]] || return 0
  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ "$line" =~ ^[[:space:]]*# ]] && continue
    [[ "$line" =~ ^[[:space:]]*$ ]] && continue
    if [[ "$line" =~ ^([A-Za-z_][A-Za-z0-9_]*)=(.*)$ ]]; then
      local key="${BASH_REMATCH[1]}"
      local value="${BASH_REMATCH[2]}"
      value="${value%\"}"
      value="${value#\"}"
      value="${value%\'}"
      value="${value#\'}"
      if [[ -z "${!key:-}" ]]; then
        export "${key}=${value}"
      fi
    fi
  done < "$file"
}

load_env_file "${ROOT}/.env.local"
load_env_file "${ROOT}/.env"

SILA_PG_HOST="${SILA_PG_HOST:-127.0.0.1}"
SILA_PG_PORT="${SILA_PG_PORT:-5432}"
SILA_PG_USER="${SILA_PG_USER:-sila}"
SILA_PG_DATABASE="${SILA_PG_DATABASE:-sila_me}"
SILA_PG_PASSWORD="${SILA_PG_PASSWORD:-sila}"

if [[ -n "${DATABASE_URL:-}" ]]; then
  while IFS= read -r line; do
    export "$line"
  done < <(python3 - "$DATABASE_URL" <<'PY'
import sys, urllib.parse
url = urllib.parse.urlparse(sys.argv[1])
if url.hostname:
    print(f"SILA_PG_HOST={url.hostname}")
if url.port:
    print(f"SILA_PG_PORT={url.port}")
if url.username:
    print(f"SILA_PG_USER={url.username}")
if url.password:
    print(f"SILA_PG_PASSWORD={urllib.parse.unquote(url.password)}")
db = (url.path or "").lstrip("/")
if db:
    print(f"SILA_PG_DATABASE={db.split('?')[0]}")
PY
)
fi

export PGPASSWORD="${SILA_PG_PASSWORD}"
export PGHOST="${SILA_PG_HOST}"
export PGPORT="${SILA_PG_PORT}"
export PGUSER="${SILA_PG_USER}"
export PGDATABASE="${SILA_PG_DATABASE}"

compose() {
  if docker compose version >/dev/null 2>&1; then
    docker compose -f "$COMPOSE_FILE" "$@"
  else
    docker-compose -f "$COMPOSE_FILE" "$@"
  fi
}

docker_available() {
  command -v docker >/dev/null 2>&1
}

usage() {
  cat <<'EOF'
SILA ME local PostgreSQL maintenance

  ./scripts/db.sh up                 Start Postgres 16 via Docker Compose
  ./scripts/db.sh down               Stop the Docker Postgres container (keeps volume)
  ./scripts/db.sh status             Safe DB status (no passwords or hashes)
  ./scripts/db.sh restore [file]     Restore a pg_dump custom dump into SILA_PG_DATABASE
  ./scripts/db.sh backup [file]      Write a local dump (default dumps/local/sila-me.dump, gitignored)
  ./scripts/db.sh snapshot           Dump platform + tenant databases into dumps/snapshots/ (committed)
  ./scripts/db.sh restore-snapshots  Recreate and restore dumps/snapshots/*.dump
  ./scripts/db.sh migrate            Apply EF Core migrations only (no user reset)
  ./scripts/db.sh psql               Open psql to the maintained database

Mac notes:
  Native Homebrew Postgres and Docker cannot both bind 5432.
  If port 5432 is already in use, start Docker on 5433:

    SILA_PG_PORT=5433 ./scripts/db.sh up
    DATABASE_URL=postgresql://sila:sila@127.0.0.1:5433/sila_me

  Put DATABASE_URL in .env (gitignored). Do not commit dumps/local or passwords.
  Restore does not recreate or reset bala@chervic.in.
EOF
}

cmd_up() {
  if ! docker_available; then
    echo "Docker is not available. Start native Postgres and set DATABASE_URL instead."
    exit 1
  fi
  mkdir -p "$DUMPS_DIR"
  SILA_PG_PORT="${SILA_PG_PORT}" POSTGRES_USER="${SILA_PG_USER}" POSTGRES_PASSWORD="${SILA_PG_PASSWORD}" POSTGRES_DB="${SILA_PG_DATABASE}" \
    compose up -d postgres
  echo "Waiting for Postgres..."
  for _ in $(seq 1 30); do
    if psql -c 'SELECT 1' >/dev/null 2>&1; then
      echo "Postgres is ready on port ${SILA_PG_PORT} database ${SILA_PG_DATABASE}."
      return 0
    fi
    sleep 1
  done
  echo "Postgres did not become ready. Check docker logs and whether port ${SILA_PG_PORT} is already used by native Postgres."
  exit 1
}

cmd_down() {
  if ! docker_available; then
    echo "Docker is not available."
    exit 1
  fi
  compose stop postgres
  echo "Stopped Docker Postgres. Data volume sila_me_pgdata was kept."
}

cmd_status() {
  if ! psql -c 'SELECT 1' >/dev/null 2>&1; then
    echo "DATABASE: UNREACHABLE (host=${SILA_PG_HOST} port=${SILA_PG_PORT} database=${SILA_PG_DATABASE} user=${SILA_PG_USER})"
    exit 1
  fi
  echo "DATABASE: CONNECTED"
  echo "PROVIDER: PostgreSQL"
  echo "HOST: ${SILA_PG_HOST}"
  echo "PORT: ${SILA_PG_PORT}"
  echo "NAME: ${SILA_PG_DATABASE}"
  echo "USER: ${SILA_PG_USER}"
  psql -At -F $'\t' -c "SELECT 'users', COUNT(*) FROM users UNION ALL SELECT 'organizations', COUNT(*) FROM organizations UNION ALL SELECT 'suppliers', COUNT(*) FROM suppliers UNION ALL SELECT 'purchase_orders', COUNT(*) FROM purchase_orders UNION ALL SELECT 'purchase_order_items', COUNT(*) FROM purchase_order_items;" \
    | awk '{print toupper($1) ": " $2}'
  echo "LATEST_MIGRATION: $(psql -At -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;')"
  local bala
  bala="$(psql -At -F $'\t' -c "SELECT \"Id\"::text, \"Status\"::text FROM users WHERE \"NormalizedEmail\"='BALA@CHERVIC.IN';")"
  if [[ -z "$bala" ]]; then
    echo "BALA: MISSING"
  else
    echo "BALA_USER_ID: ${bala%%$'\t'*}"
    echo "BALA_STATUS: ${bala#*$'\t'}"
    psql -At -c "SELECT 'BALA_ACCESS: ' || a.\"Application\"::text || '=' || a.\"Status\"::text FROM user_application_access a JOIN users u ON u.\"Id\"=a.\"UserId\" WHERE u.\"NormalizedEmail\"='BALA@CHERVIC.IN' ORDER BY 1;"
    echo "BALA_PASSWORD_HASH: $(psql -At -c "SELECT CASE WHEN length(\"PasswordHash\") > 0 THEN 'SET' ELSE 'MISSING' END FROM users WHERE \"NormalizedEmail\"='BALA@CHERVIC.IN';")"
  fi
}

cmd_restore() {
  local dump="$DEFAULT_DUMP"
  [[ -n "${1:-}" ]] && dump="$1"
  if [[ ! -f "$dump" ]]; then
    echo "Dump not found: ${dump}"
    echo "Place a dump in dumps/local/ or dumps/snapshots/ or pass the path: ./scripts/db.sh restore /path/to/sila-me.dump"
    exit 1
  fi
  if ! psql -c 'SELECT 1' >/dev/null 2>&1; then
    echo "Database is unreachable. Start it with ./scripts/db.sh up or native Postgres first."
    exit 1
  fi
  echo "Restoring custom dump into ${SILA_PG_DATABASE} (clean, no owner rewrite, no user reset after restore)."
  psql -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid();" >/dev/null
  set +e
  pg_restore --clean --if-exists --no-owner --no-acl --dbname="${SILA_PG_DATABASE}" "$dump"
  local restore_status=$?
  set -e
  if [[ "$restore_status" -gt 1 ]]; then
    echo "Restore failed with status ${restore_status}."
    exit "$restore_status"
  fi
  echo "Restore finished."
  cmd_status
}

cmd_backup() {
  mkdir -p "$LOCAL_DUMPS_DIR"
  local out="$DEFAULT_DUMP"
  [[ -n "${1:-}" ]] && out="$1"
  pg_dump --format=custom --no-owner --no-acl --file="$out" "${SILA_PG_DATABASE}"
  echo "Wrote local backup to ${out}"
}

cmd_snapshot() {
  mkdir -p "$SNAPSHOTS_DIR"
  local manifest="${SNAPSHOTS_DIR}/MANIFEST.txt"
  {
    echo "SILA ME database snapshots"
    echo "created_utc=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    echo "pg_dump=$(pg_dump --version)"
    echo "host=${SILA_PG_HOST} port=${SILA_PG_PORT}"
  } > "$manifest"
  for name in "${SNAPSHOT_DATABASES[@]}"; do
    if ! psql -d postgres -At -c "SELECT 1 FROM pg_database WHERE datname='${name}'" | grep -q 1; then
      echo "Skipping missing database ${name}"
      continue
    fi
    local out="${SNAPSHOTS_DIR}/${name}.dump"
    pg_dump --format=custom --no-owner --no-acl --compress=9 --file="$out" "$name"
    local size
    size="$(psql -d postgres -At -c "SELECT pg_size_pretty(pg_database_size('${name}'))")"
    echo "${name} ${size} -> ${out}"
    echo "${name} size=${size} file=$(basename "$out")" >> "$manifest"
  done
  echo "Wrote snapshots under ${SNAPSHOTS_DIR}"
}

cmd_restore_snapshots() {
  if [[ ! -d "$SNAPSHOTS_DIR" ]]; then
    echo "No snapshots directory at ${SNAPSHOTS_DIR}"
    exit 1
  fi
  if ! psql -d postgres -c 'SELECT 1' >/dev/null 2>&1; then
    echo "Database is unreachable. Start it with ./scripts/db.sh up or native Postgres first."
    exit 1
  fi
  for dump in "$SNAPSHOTS_DIR"/*.dump; do
    [[ -f "$dump" ]] || continue
    local name
    name="$(basename "$dump" .dump)"
    echo "Restoring ${name} from ${dump}"
    psql -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${name}' AND pid <> pg_backend_pid();" >/dev/null
    psql -d postgres -c "CREATE DATABASE ${name} OWNER ${SILA_PG_USER};" >/dev/null 2>&1 || true
    set +e
    pg_restore --clean --if-exists --no-owner --no-acl --dbname="${name}" "$dump"
    local restore_status=$?
    set -e
    if [[ "$restore_status" -gt 1 ]]; then
      echo "Restore of ${name} failed with status ${restore_status}."
      exit "$restore_status"
    fi
  done
  echo "Snapshot restore finished."
}

cmd_migrate() {
  (cd "${ROOT}/apps/api" && dotnet ef database update --no-build 2>/dev/null || dotnet ef database update)
  echo "Migrations applied. Existing users were not reset."
}

cmd_psql() {
  exec psql
}

case "${1:-}" in
  up) cmd_up ;;
  down) cmd_down ;;
  status) cmd_status ;;
  restore) cmd_restore "${2-}" ;;
  backup) cmd_backup "${2-}" ;;
  snapshot) cmd_snapshot ;;
  restore-snapshots) cmd_restore_snapshots ;;
  migrate) cmd_migrate ;;
  psql) cmd_psql ;;
  -h|--help|help|"") usage ;;
  *) echo "Unknown command: $1"; usage; exit 1 ;;
esac
