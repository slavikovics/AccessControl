#!/usr/bin/env bash
set -euo pipefail

BASE="${BASE:-http://localhost:5090}"
OUT="${OUT:-./dumps}"
mkdir -p "$OUT"

PID="${PID:-}"
if [[ -z "$PID" ]]; then
  PID="$(pgrep -f 'bin/[Dd]ebug/net10.0/ProvidedAlgorithm$' | head -1)"
fi
if [[ -z "$PID" ]]; then
  echo "ProvidedAlgorithm process not found -- start it first: cd Lab3.2-ProvidedAlgorithm && dotnet run" >&2
  exit 1
fi
echo "Using PID=$PID ($(ps -o args= -p "$PID"))"

echo "== a) create -- confidential + public records =="
CONF=$(curl -s -X POST "$BASE/confidential" -H 'Content-Type: application/json' \
  -d '{"title":"secret-note","content":"CONFIDENTIAL-PLAINTEXT-MARKER-AXQ7391"}')
PUB=$(curl -s -X POST "$BASE/public" -H 'Content-Type: application/json' \
  -d '{"title":"public-note","content":"PUBLIC-PLAINTEXT-MARKER-BXQ7391"}')
CONF_ID=$(echo "$CONF" | jq -r .id)
PUB_ID=$(echo "$PUB" | jq -r .id)
dotnet-dump collect -p "$PID" -o "$OUT/1-after-create.dmp"

echo "== b) update -- confidential gets a V2 marker (no /public update endpoint exists) =="
curl -s -o /dev/null -X PUT "$BASE/confidential/$CONF_ID" -H 'Content-Type: application/json' \
  -d '{"title":"secret-note","content":"CONFIDENTIAL-PLAINTEXT-MARKER-V2-CYT8402"}'
dotnet-dump collect -p "$PID" -o "$OUT/2-after-update.dmp"

echo "== c) delete -- confidential removed (no /public delete endpoint exists), then a full GC is forced =="
curl -s -o /dev/null -X DELETE "$BASE/confidential/$CONF_ID"
dotnet-gcdump collect -p "$PID" -o "$OUT/forced-gc.gcdump"
dotnet-dump collect -p "$PID" -o "$OUT/3-after-delete.dmp"

echo
echo "=== Marker occurrences per dump (strings | grep -c) ==="
for dump in 1-after-create 2-after-update 3-after-delete; do
  echo "-- $dump.dmp --"
  for marker in CONFIDENTIAL-PLAINTEXT-MARKER-AXQ7391 CONFIDENTIAL-PLAINTEXT-MARKER-V2-CYT8402 \
                PUBLIC-PLAINTEXT-MARKER-BXQ7391; do
    n=$(strings "$OUT/$dump.dmp" | grep -c "$marker" || true)
    echo "  $marker: $n"
  done
done

rm -rf "$OUT"
echo
echo "Dumps analyzed and deleted from $OUT."
