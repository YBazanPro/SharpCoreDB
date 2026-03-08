#!/usr/bin/env bash
# SharpCoreDB Server - Linux Installation Script
# Usage: sudo bash install.sh
# Requires: .NET 10 runtime

set -euo pipefail

INSTALL_DIR="/opt/sharpcoredb"
DATA_DIR="${INSTALL_DIR}/data"
LOG_DIR="${INSTALL_DIR}/logs"
CERT_DIR="${INSTALL_DIR}/certs"
SECRET_DIR="${INSTALL_DIR}/secrets"
SERVICE_USER="sharpcoredb"
SERVICE_FILE="/etc/systemd/system/sharpcoredb.service"

echo "╔══════════════════════════════════════════╗"
echo "║  SharpCoreDB Server v1.5.0 - Installer   ║"
echo "╚══════════════════════════════════════════╝"

# Check root
if [[ $EUID -ne 0 ]]; then
    echo "ERROR: This script must be run as root (sudo)"
    exit 1
fi

# Check .NET runtime
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET runtime not found. Install .NET 10:"
    echo "  https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version 2>/dev/null || echo "unknown")
echo "• .NET version: ${DOTNET_VERSION}"

# Create service user
if ! id "${SERVICE_USER}" &>/dev/null; then
    echo "• Creating service user: ${SERVICE_USER}"
    useradd --system --no-create-home --shell /bin/false "${SERVICE_USER}"
fi

# Create directories
echo "• Creating directories"
mkdir -p "${INSTALL_DIR}" "${DATA_DIR}/system" "${DATA_DIR}/user" \
         "${LOG_DIR}" "${CERT_DIR}" "${SECRET_DIR}"

# Copy application files
echo "• Copying application files to ${INSTALL_DIR}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

if [[ -d "${SCRIPT_DIR}/publish" ]]; then
    cp -r "${SCRIPT_DIR}/publish/"* "${INSTALL_DIR}/"
else
    echo "  WARNING: No publish directory found. Run:"
    echo "  dotnet publish src/SharpCoreDB.Server -c Release -o publish"
    echo "  Then re-run this installer."
fi

# Set permissions
echo "• Setting permissions"
chown -R "${SERVICE_USER}:${SERVICE_USER}" "${INSTALL_DIR}"
chmod 750 "${INSTALL_DIR}"
chmod 700 "${SECRET_DIR}" "${CERT_DIR}"

# Install systemd service
echo "• Installing systemd service"
cp "${SCRIPT_DIR}/sharpcoredb.service" "${SERVICE_FILE}"
systemctl daemon-reload
systemctl enable sharpcoredb.service

# Firewall rules (if ufw is available)
if command -v ufw &> /dev/null; then
    echo "• Configuring firewall (ufw)"
    ufw allow 5001/tcp comment "SharpCoreDB gRPC"
    ufw allow 8443/tcp comment "SharpCoreDB HTTPS API"
fi

echo ""
echo "═══════════════════════════════════════════"
echo "  Installation complete!"
echo ""
echo "  Next steps:"
echo "  1. Place TLS certificate:  ${CERT_DIR}/server.pfx"
echo "  2. Edit configuration:     ${INSTALL_DIR}/appsettings.json"
echo "  3. Set JWT secret:         Server__Security__JwtSecretKey=..."
echo "  4. Start server:           sudo systemctl start sharpcoredb"
echo "  5. Check status:           sudo systemctl status sharpcoredb"
echo "  6. View logs:              sudo journalctl -u sharpcoredb -f"
echo ""
echo "  Endpoints:"
echo "    gRPC:       https://localhost:5001"
echo "    REST API:   https://localhost:8443"
echo "    Health:     https://localhost:8443/health"
echo "═══════════════════════════════════════════"
