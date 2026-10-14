#!/usr/bin/env bash
# Build and launch Moth-Eidol straight from the project sources (macOS / Linux).
# Usage: scripts/run.sh [-c Release] [-- app args]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK not found. Install it from https://dotnet.microsoft.com/download" >&2
  exit 1
fi

cd "$ROOT"
exec dotnet run --project src/MothEidol.App "$@"
