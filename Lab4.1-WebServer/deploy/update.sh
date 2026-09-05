#!/usr/bin/env bash
# Redeploys just the built frontend + Caddyfile to an already-provisioned
# server (see install.sh for first-time setup), then reloads Caddy without
# dropping connections.
#
# Usage: bash deploy/update.sh user@host
set -euo pipefail

TARGET="${1:?Usage: bash deploy/update.sh user@host}"
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

echo "== Building frontend =="
( cd "$REPO_ROOT/testpage" && npm run build )

echo "== Syncing dist/ and Caddyfile to $TARGET =="
rsync -az --delete "$REPO_ROOT/testpage/dist/" "$TARGET:/tmp/lab4-dist/"
scp "$REPO_ROOT/Caddyfile" "$TARGET:/tmp/lab4-Caddyfile"

ssh "$TARGET" 'sudo rsync -a --delete /tmp/lab4-dist/ /var/www/lab4/dist/ && \
  sudo chown -R caddy:caddy /var/www/lab4 && \
  sudo install -m 0644 /tmp/lab4-Caddyfile /etc/caddy/Caddyfile && \
  sudo systemctl reload caddy && \
  rm -rf /tmp/lab4-dist /tmp/lab4-Caddyfile'

echo "Done."
