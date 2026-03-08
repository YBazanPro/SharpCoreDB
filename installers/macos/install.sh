#!/usr/bin/env bash
# SharpCoreDB Server - macOS Installation Script
# Usage: sudo bash install.sh
# Requires: .NET 10 runtime

set -euo pipefail

INSTALL_DIR="/usr/local/opt/sharpcoredb"
DATA_DIR="${INSTALL_DIR}/data"
LOG_DIR="${INSTALL_DIR}/logs"
CERT_DIR="${INSTALL_DIR}/certs"
SECRET_DIR="${INSTALL_DIR}/secrets"
SERVICE_USER="_sharpcoredb"
PLIST_NAME="com.sharpcoredb.server"
PLIST_DEST="/Library/LaunchDaemons/${PLIST_NAME}.plist"

echo "╔══════════════════════════════════════════╗"
echo "║  SharpCoreDB Server v1.5.0 - Installer   ║"
echo "║  macOS (launchd)                          ║"
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
    echo "  or:  brew install dotnet-sdk"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version 2>/dev/null || echo "unknown")
echo "• .NET version: ${DOTNET_VERSION}"

# Create service user (macOS uses dscl)
if ! dscl . -read "/Users/${SERVICE_USER}" &>/dev/null 2>&1; then
    echo "• Creating service user: ${SERVICE_USER}"
    # Find an unused UID in the system range (400-499)
    NEXT_UID=400
    while dscl . -list /Users UniqueID | awk '{print $2}' | grep -q "^${NEXT_UID}$"; do
        NEXT_UID=$((NEXT_UID + 1))
    done
    dscl . -create "/Users/${SERVICE_USER}"
    dscl . -create "/Users/${SERVICE_USER}" UniqueID "${NEXT_UID}"
    dscl . -create "/Users/${SERVICE_USER}" PrimaryGroupID 20
    dscl . -create "/Users/${SERVICE_USER}" UserShell /usr/bin/false
    dscl . -create "/Users/${SERVICE_USER}" RealName "SharpCoreDB Server"
    dscl . -create "/Users/${SERVICE_USER}" NFSHomeDirectory /var/empty
    echo "  Created user ${SERVICE_USER} (UID ${NEXT_UID})"
fi

# Create directories
echo "• Creating directories"
mkdir -p "${INSTALL_DIR}" "${DATA_DIR}/system" "${DATA_DIR}/user" \
         "${LOG_DIR}" "${CERT_DIR}" "${SECRET_DIR}"

# Copy application files
echo "• Copying application files to ${INSTALL_DIR}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

if [[ -d "${SCRIPT_DIR}/publish" ]]; then
    cp -R "${SCRIPT_DIR}/publish/" "${INSTALL_DIR}/"
else
    echo "  WARNING: No publish directory found. Run:"
    echo "  dotnet publish src/SharpCoreDB.Server -c Release -r osx-arm64 -o installers/macos/publish"
    echo "  Then re-run this installer."
fi

# Set permissions
echo "• Setting permissions"
chown -R "${SERVICE_USER}:staff" "${INSTALL_DIR}"
chmod 750 "${INSTALL_DIR}"
chmod 700 "${SECRET_DIR}" "${CERT_DIR}"

# Install launchd plist
echo "• Installing launchd service"
if launchctl list "${PLIST_NAME}" &>/dev/null 2>&1; then
    echo "  Stopping existing service"
    launchctl unload "${PLIST_DEST}" 2>/dev/null || true
fi
cp "${SCRIPT_DIR}/com.sharpcoredb.server.plist" "${PLIST_DEST}"
chown root:wheel "${PLIST_DEST}"
chmod 644 "${PLIST_DEST}"

# macOS firewall note
echo ""
echo "  NOTE: macOS does not use ufw. If the built-in firewall is active,"
echo "  allow incoming connections for 'dotnet' in System Settings > Network > Firewall."

echo ""
echo "═══════════════════════════════════════════"
echo "  Installation complete!"
echo ""
echo "  Next steps:"
echo "  1. Place TLS certificate:  ${CERT_DIR}/server.pfx"
echo "  2. Edit configuration:     ${INSTALL_DIR}/appsettings.json"
echo "  3. Set JWT secret:         Server__Security__JwtSecretKey=..."
echo "  4. Load service:           sudo launchctl load ${PLIST_DEST}"
echo "  5. Check status:           sudo launchctl list | grep sharpcoredb"
echo "  6. View logs:              tail -f ${LOG_DIR}/sharpcoredb-stdout.log"
echo ""
echo "  Service management:"
echo "    Start:    sudo launchctl load ${PLIST_DEST}"
echo "    Stop:     sudo launchctl unload ${PLIST_DEST}"
echo "    Restart:  sudo launchctl unload ${PLIST_DEST} && sudo launchctl load ${PLIST_DEST}"
echo ""
echo "  Endpoints:"
echo "    gRPC:       https://localhost:5001"
echo "    REST API:   https://localhost:8443"
echo "    Health:     https://localhost:8443/health"
echo "═══════════════════════════════════════════"
