# Lab 2.1 — SecureApp

A minimal user-data application (register / login / logout, CRUD + search over
both confidential and non-confidential records) built to demonstrate and
analyze application-level security controls, per `tasks/2.1.pdf`.

- `Backend/SecureApp.Api` — ASP.NET Core (.NET) Web API, SQLite via EF Core.
- `Frontend/secure-app-ui` — React + TypeScript + Material UI.

## Running

Backend (from `Backend/SecureApp.Api`):

```
dotnet run
```

Listens on `http://localhost:5080` by default (see `appsettings.json`). Dev-only
JWT/encryption keys live in `appsettings.Development.json` — production
deployments must supply `Jwt__Key` and `Encryption__Key` as environment
variables instead of committing real secrets.

Frontend (from `Frontend/secure-app-ui`):

```
npm install
npm run dev
```

Opens on `http://localhost:5173` (or the next free port — set
`Frontend__Origin` on the backend to match if it differs, since CORS is
locked to a single named origin).

## Security controls implemented

- Passwords hashed with PBKDF2-HMACSHA256 (210k iterations, per-user salt) —
  never stored or logged in plain text.
- Session token is a JWT delivered as an `HttpOnly; Secure; SameSite=Strict`
  cookie, never exposed to page JavaScript, mitigating token theft via XSS.
- Every confidential/public record endpoint is `[Authorize]`-protected and
  additionally filtered by `OwnerId` — one user can never read, edit, list or
  delete another user's data (verified: cross-user reads return `404`, not
  `403`, to avoid confirming a record ID exists at all).
- Confidential record title/content are encrypted at rest with AES-256-GCM
  before being written to the database; the corresponding non-confidential
  table stores plain text, so the two are directly comparable in the report.
- Brute-force mitigation: after 5 failed logins for a username, further
  attempts are rejected for a cooldown window; failed login and unknown
  username return the identical message, preventing user enumeration.
- CORS is restricted to a single named frontend origin with credentials,
  rather than a wildcard.
- Baseline hardening response headers (`X-Content-Type-Options`,
  `X-Frame-Options`, `Referrer-Policy`).
- All data access goes through parameterized EF Core queries — no
  string-concatenated SQL, so the API is not SQL-injectable.
