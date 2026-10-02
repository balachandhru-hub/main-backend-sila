#!/usr/bin/env bash
# Runs the SILA ME .NET API. EF Core migrations are applied automatically on
# startup. A development admin account is seeded when a password is provided.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

export DATABASE_URL="${DATABASE_URL:-postgresql://sila:sila@127.0.0.1:5432/sila_me}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export PORT="${PORT:-8080}"
# Dev-only seed account (ignored outside the Development environment). Override
# via a secret in production-like setups.
export SILA_ME_CLOUD_ADMIN_PASSWORD="${SILA_ME_CLOUD_ADMIN_PASSWORD:-DevPass123!}"

exec dotnet run --project apps/api/SilaMe.Api.csproj --urls "http://*:${PORT}"
