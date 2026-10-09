#!/usr/bin/env bash
set -e
BASE=${BASE:-http://localhost:5080}
OUT=${OUT:-./dumps}
mkdir -p "$OUT"
PID=${PID:-$(pgrep -x SecureApp.Api | head -1)}
[ -n "$PID" ] || { echo "SecureApp.Api is not running"; exit 1; }

curl -s -X POST $BASE/api/auth/register -H 'Content-Type: application/json' \
  -d '{"username":"dumpuser","password":"DumpUserPass123"}' >/dev/null || true
curl -s -c cookies.txt -X POST $BASE/api/auth/login -H 'Content-Type: application/json' \
  -d '{"username":"dumpuser","password":"DumpUserPass123"}' >/dev/null

send() { curl -s -b cookies.txt -X "$1" "$BASE/api/memory/$2" -H 'Content-Type: application/json' -d "$3"; }

echo "1. create"
send POST confidential '{"title":"secret","content":"CONFIDENTIAL-MARKER-1"}' >/dev/null
send POST public '{"title":"open","content":"PUBLIC-MARKER-1"}' >/dev/null
dotnet-dump collect -p $PID -o $OUT/1-create.dmp

echo "2. update"
send PUT confidential/1 '{"title":"secret","content":"CONFIDENTIAL-MARKER-2"}' >/dev/null
send PUT public/1 '{"title":"open","content":"PUBLIC-MARKER-2"}' >/dev/null
dotnet-dump collect -p $PID -o $OUT/2-update.dmp

echo "3. delete"
send DELETE confidential/1 >/dev/null
send DELETE public/1 >/dev/null
dotnet-dump collect -p $PID -o $OUT/3-delete.dmp

rm cookies.txt
count() { echo "$(strings $2 | grep -o $1 | wc -l) / $(strings -e l $2 | grep -o $1 | wc -l)"; }
echo "Marker counts, UTF-8 / UTF-16:"
for d in $OUT/*.dmp; do
  echo "$d: CONFIDENTIAL=$(count CONFIDENTIAL-MARKER $d)  PUBLIC=$(count PUBLIC-MARKER $d)"
done
