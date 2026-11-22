#!/usr/bin/env bash
# Creates a zip of the project, excluding bin, obj, .git, .idea, user settings, *.db and this script.
# Usage: ./zip-project.sh [output.zip]
set -euo pipefail

cd "$(dirname "$0")"

OUT="${1:-../$(basename "$PWD").zip}"
rm -f "$OUT"

zip -r "$OUT" . \
  -x "*/bin/*" "bin/*" \
  -x "*/obj/*" "obj/*" \
  -x ".git/*" \
  -x ".idea/*" \
  -x "*.DotSettings.user" \
  -x "*.db" \
  -x "zip-project.sh" \
  -x "*.DS_Store"

echo "Created $OUT"
