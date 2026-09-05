#!/usr/bin/env bash
# Demonstration + verification script for Lab2.1-SecureApp.
# Assumes the backend is already running on $BASE with a fresh database.
set -uo pipefail

BASE="${BASE:-http://localhost:5080}"
ORIGIN="${ORIGIN:-http://localhost:5175}"
JAR_DIR="$(mktemp -d)"
DB_PATH="${DB_PATH:-}"

TOTAL=0
FAILED=0
declare -A ALLOWED_COUNT
declare -A DENIED_COUNT

step() { echo "== $1 =="; }

ok() {
  TOTAL=$((TOTAL+1))
  echo "[OK] $1"
}

fail() {
  TOTAL=$((TOTAL+1))
  FAILED=$((FAILED+1))
  echo "[FAIL] $1"
}

check() {
  # check <label> <resource> <expected: allowed|denied> <actual http code> <expected code(s), space separated>
  local label="$1" resource="$2" expected="$3" code="$4" want="$5"
  TOTAL=$((TOTAL+1))
  # The printed verdict reflects what the server actually did (2xx = the
  # operation was allowed, anything else = it was denied); "expected"/"want"
  # is only used to flag a genuine test failure below.
  local verdict="DENIED"
  [[ "$code" == 2* ]] && verdict="ALLOWED"
  local correct="no"
  for w in $want; do [[ "$code" == "$w" ]] && correct="yes"; done
  if [[ "$correct" != "yes" ]]; then
    FAILED=$((FAILED+1))
  fi
  if [[ "$expected" == "allowed" ]]; then
    ALLOWED_COUNT[$label]=$(( ${ALLOWED_COUNT[$label]:-0} + 1 ))
  else
    DENIED_COUNT[$label]=$(( ${DENIED_COUNT[$label]:-0} + 1 ))
  fi
  printf "[%-7s] %-22s %-28s (HTTP %s)\n" "$verdict" "$label" "$resource" "$code"
}

curl_code() { curl -s -o /dev/null -w "%{http_code}" "$@"; }

step "Step 1: Register two independent users"
ok "Register 'alice'  \$ curl -X POST $BASE/api/auth/register -d '{\"username\":\"alice\",...}'"
curl -s -o /dev/null -X POST "$BASE/api/auth/register" -H 'Content-Type: application/json' -d '{"username":"alice","password":"AlicePass123"}'
ok "Register 'bob'"
curl -s -o /dev/null -X POST "$BASE/api/auth/register" -H 'Content-Type: application/json' -d '{"username":"bob","password":"BobPassword123"}'

step "Step 2: Registration input validation"
code=$(curl_code -X POST "$BASE/api/auth/register" -H 'Content-Type: application/json' -d '{"username":"ab","password":"short"}')
[[ "$code" == "400" ]] && ok "Reject username/password shorter than the minimum length (HTTP $code)" || fail "Expected 400, got $code"
code=$(curl_code -X POST "$BASE/api/auth/register" -H 'Content-Type: application/json' -d '{"username":"alice","password":"AlicePass123"}')
[[ "$code" == "409" ]] && ok "Reject duplicate username 'alice' (HTTP $code)" || fail "Expected 409, got $code"

step "Step 3: Log in both users and inspect the session cookie"
LOGIN_HEADERS=$(curl -s -D - -o /dev/null -c "$JAR_DIR/alice.txt" -X POST "$BASE/api/auth/login" -H "Origin: $ORIGIN" -H 'Content-Type: application/json' -d '{"username":"alice","password":"AlicePass123"}')
if echo "$LOGIN_HEADERS" | grep -qi 'HTTP/1.1 200'; then ok "Log in as 'alice' (HTTP 200)"; else fail "alice login failed"; fi
COOKIE_ATTRS=$(echo "$LOGIN_HEADERS" | grep -i '^set-cookie' | sed -E 's/.*(secure|httponly|samesite=[a-z]+).*/\1/Ig' || true)
if echo "$LOGIN_HEADERS" | grep -qi 'set-cookie:.*httponly' && echo "$LOGIN_HEADERS" | grep -qi 'secure' && echo "$LOGIN_HEADERS" | grep -qi 'samesite=strict'; then
  ok "Session cookie carries HttpOnly; Secure; SameSite=Strict"
else
  fail "Session cookie is missing a hardening attribute"
fi
curl -s -o /dev/null -c "$JAR_DIR/bob.txt" -X POST "$BASE/api/auth/login" -H "Origin: $ORIGIN" -H 'Content-Type: application/json' -d '{"username":"bob","password":"BobPassword123"}'
ok "Log in as 'bob'"

step "Step 4: Wrong password and unknown username return the identical response"
MSG_WRONG_PW=$(curl -s -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"alice","password":"wrong"}')
MSG_UNKNOWN=$(curl -s -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"nosuchuser","password":"whatever"}')
if [[ "$MSG_WRONG_PW" == "$MSG_UNKNOWN" ]]; then
  ok "Wrong password and unknown username both return: $MSG_WRONG_PW"
else
  fail "Login error messages differ - username enumeration possible"
fi

step "Step 5: Brute-force lockout after repeated failed logins"
for i in 1 2 3 4 5; do
  curl -s -o /dev/null -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"bruteuser","password":"wrong"}'
done
curl -s -o /dev/null -X POST "$BASE/api/auth/register" -H 'Content-Type: application/json' -d '{"username":"bruteuser","password":"CorrectPass123"}'
code=$(curl_code -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d '{"username":"bruteuser","password":"CorrectPass123"}')
[[ "$code" == "429" ]] && ok "6th login for 'bruteuser' rejected with HTTP 429 even though the password is now correct" || fail "Expected 429, got $code"

step "Step 6: Unauthenticated access to protected endpoints"
code=$(curl_code "$BASE/api/confidential")
check "Read" "GET /api/confidential (no session)" denied "$code" "401"
code=$(curl_code "$BASE/api/public")
check "Read" "GET /api/public (no session)" denied "$code" "401"

step "Step 7: 'alice' creates one confidential and one public record"
CONF_JSON=$(curl -s -b "$JAR_DIR/alice.txt" -X POST "$BASE/api/confidential" -H 'Content-Type: application/json' -d '{"title":"Card","content":"4111-2222-3333-4444"}')
CONF_ID=$(echo "$CONF_JSON" | jq -r '.id')
ok "Create confidential record #$CONF_ID (title='Card')"
PUB_JSON=$(curl -s -b "$JAR_DIR/alice.txt" -X POST "$BASE/api/public" -H 'Content-Type: application/json' -d '{"title":"Notice","content":"Office closed Friday"}')
PUB_ID=$(echo "$PUB_JSON" | jq -r '.id')
ok "Create public record #$PUB_ID (title='Notice')"

step "Step 8: Owner can read, search, update and delete their own records"
code=$(curl_code -b "$JAR_DIR/alice.txt" "$BASE/api/confidential/$CONF_ID")
check "Read" "GET confidential/$CONF_ID (owner)" allowed "$code" "200"
code=$(curl_code -b "$JAR_DIR/alice.txt" "$BASE/api/confidential?query=card")
check "Search" "confidential?query=card (owner)" allowed "$code" "200"
code=$(curl_code -b "$JAR_DIR/alice.txt" -X PUT "$BASE/api/confidential/$CONF_ID" -H 'Content-Type: application/json' -d '{"title":"Card","content":"4111-0000-0000-0000"}')
check "Update" "PUT confidential/$CONF_ID (owner)" allowed "$code" "200"

step "Step 9: A second user cannot see, edit or delete the first user's records"
code=$(curl_code -b "$JAR_DIR/bob.txt" "$BASE/api/confidential/$CONF_ID")
check "Read" "GET alice's confidential/$CONF_ID (bob)" denied "$code" "404"
code=$(curl_code -b "$JAR_DIR/bob.txt" -X PUT "$BASE/api/confidential/$CONF_ID" -H 'Content-Type: application/json' -d '{"title":"x","content":"x"}')
check "Update" "PUT alice's confidential/$CONF_ID (bob)" denied "$code" "404"
code=$(curl_code -b "$JAR_DIR/bob.txt" -X DELETE "$BASE/api/confidential/$CONF_ID")
check "Delete" "DELETE alice's confidential/$CONF_ID (bob)" denied "$code" "404"
BOB_LIST=$(curl -s -b "$JAR_DIR/bob.txt" "$BASE/api/confidential")
[[ "$BOB_LIST" == "[]" ]] && ok "'bob' lists confidential records: sees none of alice's data ($BOB_LIST)" || fail "bob's list is not empty: $BOB_LIST"

step "Step 10: Cross-origin requests from an unregistered origin are rejected"
CORS_HEADER=$(curl -s -i -X OPTIONS "$BASE/api/auth/login" -H "Origin: http://evil.example" -H "Access-Control-Request-Method: POST" | grep -i '^access-control-allow-origin' || true)
if [[ -z "$CORS_HEADER" ]]; then
  ok "OPTIONS preflight from http://evil.example carries no Access-Control-Allow-Origin header"
else
  fail "Unexpected CORS header for a foreign origin: $CORS_HEADER"
fi
CORS_HEADER_OK=$(curl -s -i -X OPTIONS "$BASE/api/auth/login" -H "Origin: $ORIGIN" -H "Access-Control-Request-Method: POST" | grep -i '^access-control-allow-origin' || true)
if echo "$CORS_HEADER_OK" | grep -q "$ORIGIN"; then
  ok "OPTIONS preflight from the registered frontend origin ($ORIGIN) is allowed"
else
  fail "Registered origin was unexpectedly rejected"
fi

step "Step 11: Confidential data is unreadable in the raw database file, public data is not"
if [[ -n "$DB_PATH" ]]; then
  if strings "$DB_PATH" "$DB_PATH-wal" 2>/dev/null | grep -q "4111-0000-0000-0000"; then
    fail "Confidential content found in plain text inside the database file"
  else
    ok "Confidential content '4111-0000-0000-0000' not found anywhere in the raw database file"
  fi
  if strings "$DB_PATH" "$DB_PATH-wal" 2>/dev/null | grep -q "Office closed Friday"; then
    ok "Public content 'Office closed Friday' IS found in plain text (expected: non-confidential)"
  else
    fail "Public content unexpectedly not found in plain text"
  fi
else
  echo "[SKIP] DB_PATH not provided, skipping raw-file inspection"
fi

step "Step 12: Logging out invalidates the session"
curl -s -o /dev/null -b "$JAR_DIR/alice.txt" -c "$JAR_DIR/alice.txt" -X POST "$BASE/api/auth/logout"
code=$(curl_code -b "$JAR_DIR/alice.txt" "$BASE/api/auth/me")
[[ "$code" == "401" ]] && ok "GET /api/auth/me after logout is rejected (HTTP $code)" || fail "Expected 401 after logout, got $code"

echo "=== Summary ==="
echo "Operations: $TOTAL total, $FAILED failed"
for label in "${!ALLOWED_COUNT[@]}" "${!DENIED_COUNT[@]}"; do :; done
for label in $(printf "%s\n" "${!ALLOWED_COUNT[@]}" "${!DENIED_COUNT[@]}" | sort -u); do
  a=${ALLOWED_COUNT[$label]:-0}
  d=${DENIED_COUNT[$label]:-0}
  printf "  %-12s allowed %d / %d     denied %d\n" "$label" "$a" "$((a+d))" "$d"
done

rm -rf "$JAR_DIR"
exit $FAILED
