#!/usr/bin/env bash
# Usage: PID=<pid of NaiveVersion> ./dump_analysis.sh
# Same steps as in Lab 3.1, but against the naive version (no auth, no public update/delete).
# Run on a freshly started app: record ids are assumed to be 0.
set -e
BASE=${BASE:-http://localhost:5090}
OUT=${OUT:-./dumps}
mkdir -p "$OUT"

send() { curl -s -X "$1" "$BASE/$2" -H 'Content-Type: application/json' -d "$3"; }

echo "1. create"
send POST confidential '{"title":"secret","content":"CONFIDENTIAL-MARKER-1"}' >/dev/null
send POST public '{"title":"open","content":"PUBLIC-MARKER-1"}' >/dev/null
dotnet-dump collect -p $PID -o $OUT/1-create.dmp

echo "2. update"
send PUT confidential/0 '{"title":"secret","content":"CONFIDENTIAL-MARKER-2"}' >/dev/null
dotnet-dump collect -p $PID -o $OUT/2-update.dmp

echo "3. delete"
send DELETE confidential/0 >/dev/null
dotnet-dump collect -p $PID -o $OUT/3-delete.dmp

echo "Marker counts (strings | grep -c):"
for d in $OUT/*.dmp; do
  echo "$d: CONFIDENTIAL=$(strings $d | grep -c CONFIDENTIAL-MARKER) PUBLIC=$(strings $d | grep -c PUBLIC-MARKER)"
done
