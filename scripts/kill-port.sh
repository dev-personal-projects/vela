#!/bin/bash

# kill-port.sh
# Kills whatever process is holding a given port
# Usage: ./scripts/kill-port.sh [port]
# Default port: 8080

PORT=${1:-7071}
HEX_PORT=$(printf '%04X' "$PORT")
KILLED=false

# Strategy 1 — check /proc/net/tcp6 (IPv6)
INODE=$(grep -i "$HEX_PORT" /proc/net/tcp6 2>/dev/null | awk '{print $10}' | head -1)

# Strategy 2 — check /proc/net/tcp (IPv4) if not found
if [[ -z "$INODE" ]]; then
    INODE=$(grep -i "$HEX_PORT" /proc/net/tcp 2>/dev/null | awk '{print $10}' | head -1)
fi

# Strategy 3 — find PID via socket inode
if [[ -n "$INODE" ]]; then
    PIDS=$(find /proc -maxdepth 3 -name fd 2>/dev/null | while read fddir; do
        ls -la "$fddir" 2>/dev/null | grep "socket:\[$INODE\]" | grep -oP '/proc/\K\d+' | head -1
    done | sort -u)

    if [[ -n "$PIDS" ]]; then
        echo "$PIDS" | xargs -r kill -9
        echo "Port $PORT freed (killed PID: $PIDS)"
        KILLED=true
    fi
fi

# Strategy 4 — fallback: kill all FinanceTracker processes
if [[ "$KILLED" == false ]]; then
    PIDS=$(ps aux | grep -i "FinanceTracker\|dotnet run" | grep -v grep | awk '{print $2}')
    if [[ -n "$PIDS" ]]; then
        echo "$PIDS" | xargs -r kill -9
        echo "Port $PORT freed via process name (killed PID: $PIDS)"
        KILLED=true
    fi
fi

if [[ "$KILLED" == false ]]; then
    echo "Nothing found holding port $PORT"
fi
