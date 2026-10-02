#!/usr/bin/env bash
# Idempotent repository bootstrap for the SILA ME workspace.
# Installs system toolchains (.NET SDK, PostgreSQL, tesseract), JS workspace
# dependencies, and builds the .NET API. Safe to run repeatedly.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# --- System dependencies -----------------------------------------------------
# The .NET 8 SDK builds/runs the API, PostgreSQL 16 backs it, and tesseract
# provides the OCR engine used by the invoice extraction pipeline.
if ! command -v dotnet >/dev/null 2>&1 \
  || ! command -v psql >/dev/null 2>&1 \
  || ! command -v tesseract >/dev/null 2>&1; then
  sudo apt-get update
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    dotnet-sdk-8.0 \
    postgresql-16 \
    postgresql-contrib \
    tesseract-ocr
fi

# --- JavaScript / TypeScript workspace --------------------------------------
pnpm install --frozen-lockfile

# --- Build the .NET API (restores NuGet packages) ---------------------------
dotnet build apps/api/SilaMe.Api.csproj -c Debug
