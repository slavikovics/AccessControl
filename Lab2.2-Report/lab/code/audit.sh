#!/usr/bin/env bash
# Black-box security audit of Lab2.1-SecureApp, per the methodology in
# section 3 of the Lab 2.2 report. Assumes the backend is already running
# on $BASE (Development mode) with a fresh database, and that no scanning
# suite (ZAP/nikto/sqlmap/gobuster) is installed in this environment --
# every phase below is a small, purpose-built script implementing the same
# category of check those tools would run.
set -uo pipefail

BASE="${BASE:-http://localhost:5080}"
DB_PATH="${DB_PATH:-}"
JAR_DIR="$(mktemp -d)"

FINDINGS=0
CHECKS=0

phase() { echo; echo "== $1 =="; }
pass() { CHECKS=$((CHECKS+1)); echo "[PASS]    $1"; }
finding() { CHECKS=$((CHECKS+1)); FINDINGS=$((FINDINGS+1)); echo "[FINDING] $1"; }
info() { echo "[INFO]    $1"; }

curl_code() { curl -s -o /dev/null -w "%{http_code}" "$@"; }

phase "Phase 1: Reconnaissance -- server banner and resource discovery"
SERVER_HDR=$(curl -s -I "$BASE/api/auth/me" | grep -i '^server:' | tr -d '\r')
if [[ -n "$SERVER_HDR" ]]; then
  finding "Server banner disclosed: '$SERVER_HDR' (aids an attacker in picking known CVEs for the stack)"
else
  pass "No Server banner disclosed"
fi

OPENAPI_CODE=$(curl_code "$BASE/openapi/v1.json")
if [[ "$OPENAPI_CODE" == "200" ]]; then
  ROUTES=$(curl -s "$BASE/openapi/v1.json" | grep -o '"/api/[a-zA-Z/{}:]*"' | sort -u | tr '\n' ' ')
  finding "OpenAPI schema publicly exposed at /openapi/v1.json (HTTP $OPENAPI_CODE) -- discloses full API surface: $ROUTES"
else
  pass "OpenAPI schema not reachable (HTTP $OPENAPI_CODE)"
fi

for p in robots.txt .env .git/config .git/HEAD swagger swagger/index.html .well-known/security.txt backup.zip web.config; do
  code=$(curl_code "$BASE/$p")
  if [[ "$code" == "200" ]]; then
    finding "Unexpected resource exposed: /$p (HTTP $code)"
  else
    pass "/$p not exposed (HTTP $code)"
  fi
done

TRACE_CODE=$(curl_code -X TRACE "$BASE/")
[[ "$TRACE_CODE" == "200" ]] && finding "TRACE method enabled (HTTP $TRACE_CODE) -- cross-site tracing risk" \
                              || pass "TRACE method not enabled (HTTP $TRACE_CODE)"

phase "Phase 2: Network / transport security headers"
HEADERS=$(curl -s -i "$BASE/api/auth/me" | tr -d '\r')
check_header() {
  local name="$1"
  if echo "$HEADERS" | grep -qi "^$name:"; then
    pass "$name header present"
  else
    finding "$name header missing"
  fi
}
check_header "X-Content-Type-Options"
check_header "X-Frame-Options"
check_header "Referrer-Policy"
check_header "Strict-Transport-Security"
check_header "Content-Security-Policy"

if echo "$HEADERS" | grep -qi "^Set-Cookie:.*secure" ; then
  : # covered separately in Phase 4
fi

phase "Phase 3: Injection testing (SQL injection, per OWASP Testing Guide WSTG-INPV)"
curl -s -o /dev/null -X POST "$BASE/api/auth/register" -H 'Content-Type: application/json' -d '{"username":"canary","password":"CanaryPass123"}'
curl -s -c "$JAR_DIR/canary.txt" -o /dev/null -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"canary","password":"CanaryPass123"}'
CANARY_ID=$(curl -s -b "$JAR_DIR/canary.txt" -X POST "$BASE/api/public" -H 'Content-Type: application/json' -d '{"title":"canary","content":"present"}' | jq -r '.id')
info "Created canary record #$CANARY_ID to detect a successful injection (e.g. a dropped/truncated table)"

SQLI_LOGIN=$(curl -s -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d "{\"username\":\"x' OR '1'='1\",\"password\":\"x\"}")
echo "$SQLI_LOGIN" | grep -q "Invalid username or password" && pass "Classic OR-based SQLi in login.username did not authenticate ($SQLI_LOGIN)" \
                                                              || finding "Unexpected login response to SQLi payload: $SQLI_LOGIN"

curl -s -b "$JAR_DIR/canary.txt" -G "$BASE/api/public" --data-urlencode "query='; DROP TABLE PublicRecords; --" >/dev/null
curl -s -b "$JAR_DIR/canary.txt" -G "$BASE/api/public" --data-urlencode "query=' UNION SELECT 1,2,3,4,5 --" >/dev/null
SURVIVED=$(curl -s -b "$JAR_DIR/canary.txt" "$BASE/api/public" | jq -r --arg id "$CANARY_ID" '.[] | select(.id==($id|tonumber)) | .id')
if [[ "$SURVIVED" == "$CANARY_ID" ]]; then
  pass "Canary record #$CANARY_ID survives DROP TABLE / UNION SELECT payloads in the search parameter -- EF Core parameterization holds"
else
  finding "Canary record #$CANARY_ID missing after SQLi payloads -- possible successful injection"
fi

phase "Phase 4: Stored XSS / output-encoding testing (OWASP WSTG-INPV-01/02)"
XSS_RESP=$(curl -s -b "$JAR_DIR/canary.txt" -X POST "$BASE/api/public" -H 'Content-Type: application/json' -d '{"title":"<script>alert(1)</script>","content":"<img src=x onerror=alert(1)>"}')
if echo "$XSS_RESP" | grep -q '<script>alert(1)</script>'; then
  finding "API stores and echoes an XSS payload verbatim, with no server-side sanitization or output encoding (not currently exploitable through the shipped React frontend, which never uses innerHTML/dangerouslySetInnerHTML -- see source review in the report -- but is a latent risk for any other client)"
else
  pass "XSS payload was sanitized or encoded by the API"
fi

phase "Phase 5: Authentication and session security"
FORGED_HDR=$(python3 - <<'PYEOF'
import base64, json, time
def b64(d): return base64.urlsafe_b64encode(d).rstrip(b'=').decode()
header = {"alg": "none", "typ": "JWT"}
payload = {"sub": "1", "unique_name": "canary", "jti": "forged", "exp": int(time.time())+3600, "iss": "SecureApp", "aud": "SecureApp"}
print(f"{b64(json.dumps(header).encode())}.{b64(json.dumps(payload).encode())}.")
PYEOF
)
FORGED_CODE=$(curl_code --cookie "access_token=$FORGED_HDR" "$BASE/api/auth/me")
[[ "$FORGED_CODE" == "401" ]] && pass "Forged alg:none JWT rejected (HTTP $FORGED_CODE) -- no JWT algorithm-confusion vulnerability" \
                               || finding "Forged alg:none JWT was accepted (HTTP $FORGED_CODE) -- critical auth bypass"

for i in 1 2 3 4 5; do curl -s -o /dev/null -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"lockouttest","password":"wrong"}'; done
LOGIN_LOCK_CODE=$(curl_code -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"lockouttest","password":"wrong"}')
[[ "$LOGIN_LOCK_CODE" == "429" ]] && pass "Login endpoint rate-limited after 5 failures (HTTP $LOGIN_LOCK_CODE)" \
                                   || finding "Login endpoint not rate-limited as expected (HTTP $LOGIN_LOCK_CODE)"

REG_CODES=""
for i in 1 2 3 4 5 6 7 8; do
  c=$(curl_code -X POST "$BASE/api/auth/register" -H 'Content-Type: application/json' -d '{"username":"canary","password":"whatever123"}')
  REG_CODES="$REG_CODES$c "
done
if echo "$REG_CODES" | grep -q "429"; then
  pass "Registration endpoint is rate-limited ($REG_CODES)"
else
  finding "Registration endpoint has NO rate limiting (8/8 unthrottled: $REG_CODES) -- allows unlimited username-enumeration probes, unlike the login endpoint"
fi

COOKIE_HDRS=$(curl -s -D - -o /dev/null -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"canary","password":"CanaryPass123"}')
if echo "$COOKIE_HDRS" | grep -qi 'set-cookie:.*httponly' && echo "$COOKIE_HDRS" | grep -qi 'secure' && echo "$COOKIE_HDRS" | grep -qi 'samesite=strict'; then
  pass "Session cookie carries HttpOnly; Secure; SameSite=Strict"
else
  finding "Session cookie is missing a hardening attribute"
fi

phase "Phase 6: Input validation / resource-exhaustion boundary"
BIG=$(python3 -c "print('A'*10050)")
OVERSIZED_CODE=$(curl_code -b "$JAR_DIR/canary.txt" -X POST "$BASE/api/public" -H 'Content-Type: application/json' -d "{\"title\":\"t\",\"content\":\"$BIG\"}")
[[ "$OVERSIZED_CODE" == "400" ]] && pass "Oversized content (10050 chars) rejected by validation (HTTP $OVERSIZED_CODE)" \
                                  || finding "Oversized content was NOT rejected (HTTP $OVERSIZED_CODE)"

phase "Phase 7: Confidentiality -- raw storage inspection"
CONF_ID=$(curl -s -b "$JAR_DIR/canary.txt" -X POST "$BASE/api/confidential" -H 'Content-Type: application/json' -d '{"title":"secret-audit-marker","content":"CONFIDENTIAL-MARKER-0xA1B2"}' | jq -r '.id')
if [[ -n "$DB_PATH" ]]; then
  if strings "$DB_PATH" "$DB_PATH-wal" 2>/dev/null | grep -q "CONFIDENTIAL-MARKER-0xA1B2"; then
    finding "Confidential marker found in plain text in the raw database file"
  else
    pass "Confidential marker not found in plain text anywhere in the raw database file (record #$CONF_ID)"
  fi
else
  info "DB_PATH not provided, skipping raw-file inspection"
fi

echo
echo "=== Summary ==="
echo "Checks: $CHECKS total, $FINDINGS findings"

rm -rf "$JAR_DIR"
exit 0
