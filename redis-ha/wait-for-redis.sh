#!/bin/sh

CONF=$1

until redis-cli -h 172.20.0.11 -p 6379 ping | grep -q PONG; do
  echo "Waiting for redis-master at 172.20.0.11:6379..."
  sleep 1
done

echo "redis-master is up. Starting Sentinel..."
exec redis-server "$CONF" --sentinel