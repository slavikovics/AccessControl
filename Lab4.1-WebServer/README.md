# Lab 4.1 — Remote server test page

A minimal React/MUI single-page site, served by Caddy with automatic HTTPS,
meant to be deployed to a real internet-facing VPS so that SSH and web-server
logs accumulate genuine background internet traffic (scanners, bruteforce
attempts) over several days for `Lab4.1-Report`'s analysis.

## Layout

- `testpage/` — Vite + React + TypeScript + MUI static site (`npm run build`
  produces `testpage/dist/`, which is what Caddy serves).
- `Caddyfile` — reads the domain from `$DOMAIN` (see `deploy/caddy.env.example`)
  so it stays generic; everything else (JSON access logging, security
  headers, gzip) is fixed.
- `deploy/install.sh` — first-time provisioning on a fresh Debian/Ubuntu
  server: installs Caddy from its official apt repo, lays down the site and
  Caddyfile, wires `/etc/caddy/caddy.env` into the systemd unit, opens the
  firewall, starts the service.
- `deploy/update.sh user@host` — rebuilds the frontend locally and pushes a
  new `dist/` + `Caddyfile` to an already-provisioned server, then reloads
  Caddy (no downtime, no re-running `install.sh`).

## First deploy

1. Point a domain's A/AAAA record at the server's public IP (Let's Encrypt
   needs a real domain — see the Lab 4.1 report/discussion for why a bare IP
   won't get a certificate through the normal flow).
2. `cd testpage && npm run build && cd ..`
3. Copy this whole directory to the server and run the installer (it reads
   `testpage/dist` relative to its own location, so keep the layout intact):
   ```
   rsync -az --exclude node_modules --exclude testpage/src . user@host:/tmp/lab4-deploy/
   ssh user@host 'sudo bash /tmp/lab4-deploy/deploy/install.sh'
   ```
4. Edit `/etc/caddy/caddy.env` on the server with the real `DOMAIN=...`,
   then `sudo systemctl restart caddy`.
5. Confirm `https://<domain>/` loads and `sudo tail -f /var/log/caddy/access.log`
   shows JSON lines.

## Later updates

```
bash deploy/update.sh user@host
```
