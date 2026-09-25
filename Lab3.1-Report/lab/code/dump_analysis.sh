#!/usr/bin/env bash
set -euo pipefail

PID="${PID:?Set PID to the SecureApp.Api process id (see: pgrep -f SecureApp.Api.dll)}"
BASE="${BASE:-http://localhost:5080}"
OUT="${OUT:-./dumps}"
JAR="$(mktemp -d)"
mkdir -p "$OUT"

curl -s -o /dev/null -X POST "$BASE/api/auth/register" -H 'Content-Type: application/json' \
  -d '{"username":"dumpuser","password":"DumpUserPass123"}' || true
curl -s -c "$JAR/v.txt" -o /dev/null -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' \
  -d '{"username":"dumpuser","password":"DumpUserPass123"}'

echo "== a) create -- confidential + public in-memory records =="
CONF=$(curl -s -b "$JAR/v.txt" -X POST "$BASE/api/memory/confidential" -H 'Content-Type: application/json' \
  -d '{"title":"secret-note","content":"CONFIDENTIAL-PLAINTEXT-MARKER-AXQ7391"}')
PUB=$(curl -s -b "$JAR/v.txt" -X POST "$BASE/api/memory/public" -H 'Content-Type: application/json' \
  -d '{"title":"public-note","content":"PUBLIC-PLAINTEXT-MARKER-BXQ7391"}')
CONF_ID=$(echo "$CONF" | jq -r .id)
PUB_ID=$(echo "$PUB" | jq -r .id)
dotnet-dump collect -p "$PID" -o "$OUT/1-after-create.dmp"

echo "== b) update -- both records get a V2 marker =="
curl -s -o /dev/null -b "$JAR/v.txt" -X PUT "$BASE/api/memory/confidential/$CONF_ID" -H 'Content-Type: application/json' \
  -d '{"title":"secret-note","content":"CONFIDENTIAL-PLAINTEXT-MARKER-V2-CYT8402"}'
curl -s -o /dev/null -b "$JAR/v.txt" -X PUT "$BASE/api/memory/public/$PUB_ID" -H 'Content-Type: application/json' \
  -d '{"title":"public-note","content":"PUBLIC-PLAINTEXT-MARKER-V2-DYT8402"}'
dotnet-dump collect -p "$PID" -o "$OUT/2-after-update.dmp"

echo "== c) delete -- both records removed, then a full GC is forced =="
curl -s -o /dev/null -b "$JAR/v.txt" -X DELETE "$BASE/api/memory/confidential/$CONF_ID"
curl -s -o /dev/null -b "$JAR/v.txt" -X DELETE "$BASE/api/memory/public/$PUB_ID"
dotnet-gcdump collect -p "$PID" -o "$OUT/forced-gc.gcdump"
dotnet-dump collect -p "$PID" -o "$OUT/3-after-delete.dmp"

echo
echo "=== Marker occurrences per dump (strings | grep -c) ==="
for dump in 1-after-create 2-after-update 3-after-delete; do
  echo "-- $dump.dmp --"
  for marker in CONFIDENTIAL-PLAINTEXT-MARKER-AXQ7391 CONFIDENTIAL-PLAINTEXT-MARKER-V2-CYT8402 \
                PUBLIC-PLAINTEXT-MARKER-BXQ7391 PUBLIC-PLAINTEXT-MARKER-V2-DYT8402; do
    n=$(strings "$OUT/$dump.dmp" | grep -c "$marker" || true)
    echo "  $marker: $n"
  done
done

rm -rf "$JAR"
rm -rf "$OUT"
echo
echo "Dumps analyzed and deleted from $OUT."
