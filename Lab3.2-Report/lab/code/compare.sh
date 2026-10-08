#!/usr/bin/env bash
# Runs the same checks against both versions.
# Needs both apps running, freshly started: SecureApp.Api (5080) and NaiveVersion (5090).
OWN=http://localhost:5080/api/memory/confidential
NAIVE=http://localhost:5090/confidential

curl -s -X POST http://localhost:5080/api/auth/register -H 'Content-Type: application/json' \
  -d '{"username":"cmpuser","password":"CmpUserPass123"}' >/dev/null
curl -s -c cookies.txt -X POST http://localhost:5080/api/auth/login -H 'Content-Type: application/json' \
  -d '{"username":"cmpuser","password":"CmpUserPass123"}' >/dev/null

post() { curl -s -b cookies.txt -o /dev/null -w '%{http_code}' -X POST "$1" -H 'Content-Type: application/json' -d "$2"; }
big() { head -c "$1" /dev/zero | tr '\0' 'a'; }
printf '{"title":"t","content":"%s"}' "$(big 5000000)" > big.json

echo "== Concurrency: 200 POSTs, 50 at a time =="
for t in "own $OWN" "naive $NAIVE"; do
  set -- $t
  ids=$(seq 200 | xargs -P50 -I{} curl -s -b cookies.txt -X POST "$2" -H 'Content-Type: application/json' \
        -d '{"title":"t","content":"c"}' | grep -o '"id":[0-9]*' | sort -u | wc -l)
  list=$(curl -s -b cookies.txt -w '\n%{http_code}' "$2")
  stored=$(echo "$list" | grep -o '"id"' | wc -l)
  echo "$1: unique ids $ids / 200, stored $stored / 200, GET list -> HTTP $(echo "$list" | tail -1)"
done

echo "== Invalid input: HTTP status =="
printf '%-28s %-6s %s\n' case own naive
check() {
  printf '%-28s %-6s %s\n' "$1" "$(post $OWN "$2")" "$(post $NAIVE "$2")"
}
check "wrong types"      '{"title":1,"content":true}'
check "broken JSON"      '{"title":'
check "missing field"    '{"title":"t"}'
check "empty body"       ''
check "10050 chars"      "{\"title\":\"t\",\"content\":\"$(big 10050)\"}"
check "5 MB"             "@big.json"
check "newline, tab"     '{"title":"t","content":"a\nb\tc"}'
rm cookies.txt big.json
