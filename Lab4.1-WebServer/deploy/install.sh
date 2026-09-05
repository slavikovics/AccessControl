#!/usr/bin/env bash
# Installs and configures Caddy for Lab 4.1 on a fresh Debian/Ubuntu server.
# Run as root (or via sudo) on the remote server itself, e.g.:
#   scp -r deploy Caddyfile user@host:/tmp/lab4-deploy
#   ssh user@host 'sudo bash /tmp/lab4-deploy/deploy/install.sh'
#
# Idempotent: safe to re-run after changing the Caddyfile or DOMAIN.
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

if [[ $EUID -ne 0 ]]; then
  echo "Run as root (sudo bash install.sh)" >&2
  exit 1
fi

echo "== Installing Caddy from the official apt repository =="
if ! command -v caddy >/dev/null 2>&1; then
  apt-get update -y
  apt-get install -y debian-keyring debian-archive-keyring apt-transport-https curl
  curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' \
    | gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
  curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' \
    | tee /etc/apt/sources.list.d/caddy-stable.list
  apt-get update -y
  apt-get install -y caddy
else
  echo "caddy already installed: $(caddy version)"
fi

echo "== Preparing site directory =="
mkdir -p /var/www/lab4
if [[ -d "$REPO_ROOT/testpage/dist" ]]; then
  rsync -a --delete "$REPO_ROOT/testpage/dist/" /var/www/lab4/dist/
else
  echo "WARNING: $REPO_ROOT/testpage/dist not found -- build the frontend first (npm run build) and re-copy it." >&2
fi
chown -R caddy:caddy /var/www/lab4

echo "== Installing Caddyfile =="
install -m 0644 "$REPO_ROOT/Caddyfile" /etc/caddy/Caddyfile

echo "== Setting up DOMAIN environment for the caddy service =="
if [[ ! -f /etc/caddy/caddy.env ]]; then
  install -m 0600 "$SCRIPT_DIR/caddy.env.example" /etc/caddy/caddy.env
  echo "Created /etc/caddy/caddy.env from the example -- edit it with the real domain before continuing:"
  echo "  sudo nano /etc/caddy/caddy.env"
fi

mkdir -p /etc/systemd/system/caddy.service.d
cat > /etc/systemd/system/caddy.service.d/override.conf <<'EOF'
[Service]
EnvironmentFile=/etc/caddy/caddy.env
EOF

echo "== Opening firewall ports (ufw, if present) =="
if command -v ufw >/dev/null 2>&1; then
  ufw allow 22/tcp || true
  ufw allow 80/tcp || true
  ufw allow 443/tcp || true
fi

echo "== Reloading systemd and (re)starting caddy =="
systemctl daemon-reload
systemctl enable caddy
systemctl restart caddy
sleep 1
systemctl --no-pager status caddy || true

echo
echo "Done. Check DOMAIN in /etc/caddy/caddy.env, make sure its DNS A/AAAA"
echo "record points at this server's public IP, then: sudo systemctl restart caddy"
echo "Logs: sudo tail -f /var/log/caddy/access.log"
