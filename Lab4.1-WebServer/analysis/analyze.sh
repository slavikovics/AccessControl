#!/usr/bin/env bash
# Usage: ssh root@212.192.9.89 'bash -s ssh' <  ./analyze.sh (ssh or web or ossec)
top() { sort | uniq -c | sort -rn | head -${1:-10}; }

ssh_report() {
  LOG=$(zcat -f /var/log/auth.log*)
  FAILED=$(echo "$LOG" | grep 'Failed password')
  echo "Period: $(echo "$LOG" | cut -c1-10 | grep -E '^20' | sort | sed -n '1p;$p' | tr '\n' ' ')"
  echo "Failed password attempts: $(echo "$FAILED" | wc -l)"
  echo "Unique source IPs: $(echo "$FAILED" | grep -oP 'from \K\S+' | sort -u | wc -l)"
  echo "Attempts for root: $(echo "$FAILED" | grep -c ' root from ')"
  echo; echo "Top 10 source IPs:"
  echo "$FAILED" | grep -oP 'from \K\S+' | top
  echo; echo "Top 10 user names:"
  echo "$FAILED" | grep -oP 'for (invalid user )?\K\S+' | top
  echo; echo "Failed attempts per day:"
  echo "$FAILED" | cut -c1-10 | sort | uniq -c
  echo; echo "Successful logins (method user ip):"
  echo "$LOG" | grep -P 'sshd.*Accepted' | grep -oP 'Accepted \K\S+ for \S+ from \S+' | top
}

web_report() {
  LOG=/var/log/caddy/access.log
  echo "Requests: $(wc -l < $LOG)   unique IPs: $(jq -r .request.client_ip $LOG | sort -u | wc -l)"
  echo; echo "Status codes:";           jq -r .status $LOG | top
  echo; echo "Top 10 client IPs:";      jq -r .request.client_ip $LOG | top
  echo; echo "Top 10 paths answered 404:"; jq -r 'select(.status==404) | .request.uri' $LOG | top
  echo; echo "Top 5 User-Agents:";      jq -r '.request.headers["User-Agent"][0] // "-"' $LOG | cut -c1-80 | top 5
}

ossec_report() {
  ALERTS=$(zcat -f /var/ossec/logs/alerts/*/*/*.log.gz /var/ossec/logs/alerts/alerts.log)
  BANS=$(grep 'firewall-drop.sh add' /var/ossec/logs/active-responses.log)
  echo "Alerts: $(echo "$ALERTS" | grep -c '^\*\* Alert')"
  echo; echo "Alerts by level:";    echo "$ALERTS" | grep -oP '^Rule: \d+ \(level \K\d+' | sort -n | uniq -c
  echo; echo "Top 8 rules:";       echo "$ALERTS" | grep -oP '^Rule: \K.*' | top 8
  echo; echo "Bans (firewall-drop add): $(echo "$BANS" | wc -l), unique IPs: $(echo "$BANS" | awk '{print $(NF-2)}' | sort -u | wc -l)"
  echo; echo "Most banned IPs:";   echo "$BANS" | awk '{print $(NF-2)}' | top
}

"${1:-ssh}"_report
