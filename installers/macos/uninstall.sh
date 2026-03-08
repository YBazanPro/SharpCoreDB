#!/usr/bin/env bash
# SharpCoreDB Server - macOS Uninstallation Script
# Usage: sudo bash uninstall.sh
# WARNING: This will permanently remove SharpCoreDB Server and all data

set -euo pipefail

INSTALL_DIR="/usr/local/opt/sharpcoredb"
SERVICE_USER="_sharpcoredb"
PLIST_NAME="com.sharpcoredb.server"
PLIST_DEST="/Library/LaunchDaemons/${PLIST_NAME}.plist"

echo "╔══════════════════════════════════════════════╗"
echo "║  SharpCoreDB Server - Uninstaller            ║"
echo "║  WARNING: This will delete all data!         ║"
echo "╚══════════════════════════════════════════════╝"

# Check root
if [[ $EUID -ne 0 ]]; then
    echo "ERROR: This script must be run as root (sudo)"
    exit 1
fi

# Confirm uninstallation
echo ""
echo "This will:"
echo "  • Stop and remove the SharpCoreDB service"
echo "  • Delete all data in ${INSTALL_DIR}"
echo "  • Remove the service user ${SERVICE_USER}"
echo ""
read -p "Are you sure you want to continue? (yes/no): " -r
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo "Uninstallation cancelled."
    exit 0
fi

# Stop and unload service
echo "• Stopping and unloading service"
if launchctl list "${PLIST_NAME}" &>/dev/null 2>&1; then
    launchctl unload "${PLIST_DEST}" 2>/dev/null || true
    echo "  Service stopped"
else
    echo "  Service not running"
fi

# Remove plist
if [[ -f "${PLIST_DEST}" ]]; then
    rm -f "${PLIST_DEST}"
    echo "  Removed launchd plist"
fi

# Remove application files
if [[ -d "${INSTALL_DIR}" ]]; then
    rm -rf "${INSTALL_DIR}"
    echo "  Removed installation directory"
fi

# Remove service user
if dscl . -read "/Users/${SERVICE_USER}" &>/dev/null 2>&1; then
    # Kill any processes owned by the user
    pgrep -u "${SERVICE_USER}" | xargs kill -9 2>/dev/null || true

    # Remove user
    dscl . -delete "/Users/${SERVICE_USER}" 2>/dev/null || true
    echo "  Removed service user ${SERVICE_USER}"
fi

# Remove from firewall (if configured)
# Note: macOS firewall rules are managed via System Settings

echo ""
echo "═══════════════════════════════════════════════"
echo "  Uninstallation complete!"
echo ""
echo "  SharpCoreDB Server has been completely removed."
echo "  The following were deleted:"
echo "    • Service configuration"
echo "    • Application files (${INSTALL_DIR})"
echo "    • Service user (${SERVICE_USER})"
echo "    • All data and logs"
echo "═══════════════════════════════════════════════"</content>
<parameter name="filePath">installers/macos/uninstall.sh
