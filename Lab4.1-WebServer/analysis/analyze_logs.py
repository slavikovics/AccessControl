#!/usr/bin/env python3
import argparse
import glob
import gzip
import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone

MONTHS = {m: i for i, m in enumerate(
    "Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec".split(), 1)}

SSH_FAILED = re.compile(
    r"Failed (?P<method>\w[\w-]*) for (?:invalid user )?(?P<user>\S+) from (?P<ip>[0-9a-fA-F:.]+)")
SSH_INVALID = re.compile(r"Invalid user (?P<user>\S*) from (?P<ip>[0-9a-fA-F:.]+)")
SSH_ACCEPTED = re.compile(
    r"Accepted (?P<method>\S+) for (?P<user>\S+) from (?P<ip>[0-9a-fA-F:.]+)")
SSH_PREAUTH = re.compile(r"(?:Connection closed|Disconnected|Connection reset).*\[preauth\]")


def open_any(path):
    if path.endswith(".gz"):
        return gzip.open(path, "rt", errors="replace")
    return open(path, "rt", errors="replace")


def files(pattern):
    return sorted(p for p in glob.glob(pattern, recursive=True) if not p.endswith(".sum"))


def parse_syslog_time(line, year):
    m = re.match(r"(\d{4}-\d{2}-\d{2})T(\d{2}):(\d{2}):(\d{2})", line[:32])
    if m:
        return datetime.strptime(f"{m[1]} {m[2]}:{m[3]}:{m[4]}", "%Y-%m-%d %H:%M:%S")
    m = re.match(r"([A-Z][a-z]{2})\s+(\d+)\s+(\d{2}):(\d{2}):(\d{2})", line)
    if m and m.group(1) in MONTHS:
        return datetime(year, MONTHS[m.group(1)], int(m.group(2)), int(m.group(3)), int(m.group(4)), int(m.group(5)))
    return None


def in_range(ts, since, until):
    if ts is None:
        return False
    if since and ts < since:
        return False
    if until and ts >= until:
        return False
    return True


def parse_bound(text):
    if not text:
        return None
    for fmt in ("%Y-%m-%dT%H:%M", "%Y-%m-%d %H:%M", "%Y-%m-%d"):
        try:
            return datetime.strptime(text, fmt)
        except ValueError:
            pass
    raise SystemExit(f"bad date/time: {text} (use YYYY-MM-DD or YYYY-MM-DDTHH:MM, server time = UTC)")


def top(counter, n, label_a, label_b="count"):
    for key, cnt in counter.most_common(n):
        print(f"  {cnt:>7}  {key}")


def ssh_report(pattern, since, until, year, topn):
    print("=" * 70)
    print("SSH  (" + pattern + ")")
    print("=" * 70)
    paths = files(pattern)
    if not paths:
        print("  (no files found)")
        return
    failed = Counter()
    ips = Counter()
    users = Counter()
    per_day = Counter()
    per_hour = Counter()
    accepted = []
    invalid_ips = Counter()
    preauth = 0
    first = last = None
    for p in paths:
        with open_any(p) as fh:
            for line in fh:
                if "sshd" not in line:
                    continue
                day = parse_syslog_time(line, year)
                if not in_range(day, since, until):
                    continue
                hour = day.hour
                first = day if first is None or day < first else first
                last = day if last is None or day > last else last
                m = SSH_ACCEPTED.search(line)
                if m:
                    accepted.append((day.date().isoformat(), m["method"], m["user"], m["ip"]))
                    continue
                m = SSH_FAILED.search(line)
                if m:
                    failed[m["method"]] += 1
                    ips[m["ip"]] += 1
                    users[m["user"]] += 1
                    per_day[day.date().isoformat()] += 1
                    per_hour[hour] += 1
                    continue
                m = SSH_INVALID.search(line)
                if m:
                    invalid_ips[m["ip"]] += 1
                    users[m["user"] or "(empty)"] += 0
                    continue
                if SSH_PREAUTH.search(line):
                    preauth += 1
    total_failed = sum(failed.values())
    print(f"Period covered by data: {first.date() if first else '-'} .. {last.date() if last else '-'}")
    print(f"Failed authentication attempts: {total_failed}   (by method: {dict(failed)})")
    print(f"'Invalid user' events (unknown login name): {sum(invalid_ips.values())}")
    print(f"Connections dropped before authentication ([preauth]): {preauth}")
    print(f"Unique source IPs with failed attempts: {len(ips)}")
    print(f"Successful logins: {len(accepted)}")
    print("\nFailed attempts per day:")
    for d in sorted(per_day):
        print(f"  {d}  {per_day[d]}")
    print("\nFailed attempts by hour of day (UTC/server time):")
    print("  " + " ".join(f"{h:02d}:{per_hour[h]}" for h in range(24)))
    print(f"\nTop {topn} source IPs (failed attempts):")
    top(ips, topn, "ip")
    print(f"\nTop {topn} attacked user names:")
    top(Counter({u: c for u, c in users.items() if c}), topn, "user")
    subnets = Counter()
    for ip, c in ips.items():
        if ":" in ip:
            subnets[":".join(ip.split(":")[:3]) + "::/48"] += c
        else:
            subnets[".".join(ip.split(".")[:3]) + ".0/24"] += c
    print(f"\nTop {topn} /24 (IPv4) or /48 (IPv6) networks:")
    top(subnets, topn, "net")
    hits_root = users.get("root", 0)
    print(f"\nAttempts against 'root': {hits_root}"
          + (f"  ({100 * hits_root / total_failed:.1f}% of all failed attempts)" if total_failed else ""))
    if accepted:
        print("\nSuccessful logins by (method, user, source IP) -- verify each is you:")
        agg = Counter((a[1], a[2], a[3]) for a in accepted)
        for (method, user, ip), cnt in agg.most_common(20):
            print(f"  {cnt:>7}  {method:<10} {user:<10} {ip}")
    print("\nIPs seen in the successful-login list AND the failed list (possible compromise or typo):")
    ok_ips = {a[3] for a in accepted}
    for ip in sorted(ok_ips & set(ips)):
        print(f"  {ip}  failed={ips[ip]}")


def caddy_report(pattern, since, until, topn):
    print("\n" + "=" * 70)
    print("CADDY  (" + pattern + ")")
    print("=" * 70)
    paths = files(pattern)
    if not paths:
        print("  (no files found)")
        return
    total = 0
    status = Counter()
    ips = Counter()
    uas = Counter()
    paths_404 = Counter()
    methods = Counter()
    per_day = Counter()
    hosts = Counter()
    real_pages = 0
    bytes_out = 0
    first_ts = None
    first_row = None
    for p in paths:
        with open_any(p) as fh:
            for line in fh:
                try:
                    r = json.loads(line)
                except ValueError:
                    continue
                ts = r.get("ts")
                if ts is None:
                    continue
                dt = datetime.fromtimestamp(float(ts), tz=timezone.utc).replace(tzinfo=None)
                if not in_range(dt, since, until):
                    continue
                req = r.get("request", {})
                ip = req.get("client_ip") or req.get("remote_ip") or "?"
                uri = req.get("uri", "")
                ua = (req.get("headers", {}).get("User-Agent") or ["(none)"])[0]
                total += 1
                st = r.get("status")
                status[st] += 1
                ips[ip] += 1
                uas[ua[:90]] += 1
                methods[req.get("method", "?")] += 1
                hosts[req.get("host", "?")] += 1
                per_day[dt.date().isoformat()] += 1
                bytes_out += r.get("size", 0) or 0
                if st == 404:
                    paths_404[uri.split("?")[0][:80]] += 1
                if uri in ("/", "/index.html") and st == 200:
                    real_pages += 1
                if first_ts is None or dt < first_ts:
                    first_ts = dt
                    first_row = (dt.isoformat() + "Z", ip, req.get("method"), uri, ua[:60])
    print(f"Total requests: {total}   response bytes: {bytes_out}")
    if first_row:
        print("First request in window:", first_row)
    print(f"Unique client IPs: {len(ips)}")
    print(f"Successful loads of the test page ('/' -> 200): {real_pages}")
    n404 = status.get(404, 0)
    print(f"Requests answered 404: {n404}" + (f" ({100 * n404 / total:.1f}%)" if total else ""))
    print("\nStatus codes:", dict(sorted(status.items(), key=lambda kv: str(kv[0]))))
    print("Methods:", dict(methods))
    print("\nRequests per day:")
    for d in sorted(per_day):
        print(f"  {d}  {per_day[d]}")
    print(f"\nTop {topn} client IPs:")
    top(ips, topn, "ip")
    print(f"\nTop {topn} User-Agents:")
    top(uas, topn, "ua")
    print(f"\nTop {topn} not-found paths (scanner fingerprints):")
    top(paths_404, topn, "path")
    print(f"\nTop hosts requested (Host header; IP-only hits = scanners not using the domain):")
    top(hosts, 5, "host")


def ossec_report(alerts, active, since, until, year, topn):
    print("\n" + "=" * 70)
    print("OSSEC")
    print("=" * 70)
    rules = Counter()
    levels = Counter()
    src = Counter()
    per_day = Counter()
    n_alerts = 0
    for p in files(alerts):
        if p.endswith(".sum"):
            continue
        with open_any(p) as fh:
            cur_day = None
            for line in fh:
                if line.startswith("** Alert"):
                    m = re.match(r"\*\* Alert (\d+)", line)
                    if m:
                        cur_day = datetime.fromtimestamp(int(m.group(1)), tz=timezone.utc).replace(tzinfo=None)
                        if not in_range(cur_day, since, until):
                            cur_day = None
                            continue
                        n_alerts += 1
                        per_day[cur_day.date().isoformat()] += 1
                elif cur_day is not None:
                    m = re.match(r"Rule: (\d+) \(level (\d+)\) -> '(.*)'", line)
                    if m:
                        rules[f"rule {m[1]} lvl {m[2]}  {m[3]}"] += 1
                        levels[int(m[2])] += 1
                    m = re.match(r"Src IP: (\S+)", line)
                    if m:
                        src[m[1]] += 1
    print(f"Alerts in window: {n_alerts}   by level: {dict(sorted(levels.items()))}")
    print("\nAlerts per day:")
    for d in sorted(per_day):
        print(f"  {d}  {per_day[d]}")
    print(f"\nTop {topn} rules:")
    top(rules, topn, "rule")
    print(f"\nTop {topn} alert source IPs:")
    top(src, topn, "ip")

    add = defaultdict(list)
    seen = set()
    dele = 0
    for p in files(active):
        with open_any(p) as fh:
            for line in fh:
                m = re.match(r"(\w{3} \w{3}\s+\d+ [\d:]+) .*?(add|delete)\s+.*?\s(\d+\.\d+\.\d+\.\d+|[0-9a-f:]+)\s", line)
                if not m:
                    m2 = re.search(r"(add|delete).*?(\d+\.\d+\.\d+\.\d+)", line)
                    if not m2:
                        continue
                    when, action, ip = "?", m2[1], m2[2]
                else:
                    when, action, ip = m[1], m[2], m[3]
                try:
                    ts = datetime.strptime(re.sub(r"\s+", " ", when.replace(" UTC", "")) + f" {year}", "%a %b %d %H:%M:%S %Y")
                    if not in_range(ts, since, until):
                        continue
                except ValueError:
                    pass
                key = (when, ip, action)
                if key in seen:
                    continue
                seen.add(key)
                if action == "add":
                    add[ip].append(when)
                else:
                    dele += 1
    total_blocks = sum(len(v) for v in add.values())
    print(f"\nActive-response block events (one per ban, host-deny + firewall-drop counted once): "
          f"{total_blocks} against {len(add)} unique IPs; unblocks (ban expired): {dele}")
    repeat = {ip: w for ip, w in add.items() if len(w) > 1}
    print(f"IPs banned more than once (returned after the 600 s ban expired): {len(repeat)} of {len(add)}")
    print(f"Top {topn} most-banned IPs:")
    for ip, whens in sorted(add.items(), key=lambda kv: -len(kv[1]))[:topn]:
        print(f"  {len(whens):>7}  {ip}  first ban: {whens[0]}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--auth", default="/var/log/auth.log*")
    ap.add_argument("--caddy", default="/var/log/caddy/access*.log*")
    ap.add_argument("--ossec-alerts", default="/var/ossec/logs/alerts/**/*.log*")
    ap.add_argument("--ossec-active", default="/var/ossec/logs/active-responses.log*")
    ap.add_argument("--no-ossec", action="store_true")
    ap.add_argument("--since", help="YYYY-MM-DD[THH:MM], inclusive, UTC")
    ap.add_argument("--until", help="YYYY-MM-DD[THH:MM], inclusive, UTC")
    ap.add_argument("--year", type=int, default=datetime.now().year,
                    help="year for classic syslog timestamps without a year")
    ap.add_argument("--top", type=int, default=10)
    a = ap.parse_args()
    since = parse_bound(a.since)
    until = parse_bound(a.until)

    print(f"Log analysis  window: {a.since or 'start'} .. {a.until or 'now'}   generated {datetime.now():%Y-%m-%d %H:%M}")
    ssh_report(a.auth, since, until, a.year, a.top)
    caddy_report(a.caddy, since, until, a.top)
    if not a.no_ossec:
        ossec_report(a.ossec_alerts, a.ossec_active, since, until, a.year, a.top)


if __name__ == "__main__":
    main()
