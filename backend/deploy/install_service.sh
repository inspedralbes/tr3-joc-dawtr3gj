#!/usr/bin/env bash
set -euo pipefail

APP_DIR="/opt/tankarena-backend"
SERVICE_PATH="/etc/systemd/system/tankarena-backend.service"

mkdir -p "$APP_DIR"
cp -R . "$APP_DIR"
cd "$APP_DIR"
npm ci --omit=dev

cp deploy/tankarena-backend.service "$SERVICE_PATH"
systemctl daemon-reload
systemctl enable tankarena-backend.service
systemctl restart tankarena-backend.service
systemctl status tankarena-backend.service --no-pager
