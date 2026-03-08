#!/usr/bin/env bash
# SharpCoreDB Server - macOS .pkg Builder
# Usage: bash build_pkg.sh
# Requires: Xcode command line tools (pkgbuild)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PKG_NAME="SharpCoreDB-Server-1.5.0"
PKG_FILE="${PKG_NAME}.pkg"
IDENTIFIER="com.sharpcoredb.server"

echo "Building macOS .pkg installer for SharpCoreDB Server"
echo "=================================================="

# Check for required tools
if ! command -v pkgbuild &> /dev/null; then
    echo "ERROR: pkgbuild not found. Install Xcode command line tools:"
    echo "  xcode-select --install"
    exit 1
fi

# Publish the application
echo "• Publishing application..."
PUBLISH_DIR="${SCRIPT_DIR}/publish"
rm -rf "${PUBLISH_DIR}"
dotnet publish "${PROJECT_ROOT}/src/SharpCoreDB.Server" \
    -c Release \
    -r osx-arm64 \
    --self-contained true \
    -p:PublishTrimmed=false \
    -o "${PUBLISH_DIR}"

# Create package root structure
PKG_ROOT="${SCRIPT_DIR}/pkgroot"
rm -rf "${PKG_ROOT}"
mkdir -p "${PKG_ROOT}/usr/local/opt/sharpcoredb"
mkdir -p "${PKG_ROOT}/Library/LaunchDaemons"

# Copy application files
echo "• Copying application files..."
cp -R "${PUBLISH_DIR}/" "${PKG_ROOT}/usr/local/opt/sharpcoredb/"

# Copy launchd plist
echo "• Copying launchd configuration..."
cp "${SCRIPT_DIR}/com.sharpcoredb.server.plist" "${PKG_ROOT}/Library/LaunchDaemons/"

# Create preinstall script
PREINSTALL_SCRIPT="${SCRIPT_DIR}/preinstall"
cat > "${PREINSTALL_SCRIPT}" << 'EOF'
#!/bin/bash
# Pre-installation script for SharpCoreDB Server

# Create service user if it doesn't exist
SERVICE_USER="_sharpcoredb"
if ! dscl . -read "/Users/${SERVICE_USER}" &>/dev/null 2>&1; then
    echo "Creating service user: ${SERVICE_USER}"
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
fi

# Create directories
mkdir -p "/usr/local/opt/sharpcoredb/data/system"
mkdir -p "/usr/local/opt/sharpcoredb/data/user"
mkdir -p "/usr/local/opt/sharpcoredb/logs"
mkdir -p "/usr/local/opt/sharpcoredb/certs"
mkdir -p "/usr/local/opt/sharpcoredb/secrets"

exit 0
EOF
chmod +x "${PREINSTALL_SCRIPT}"

# Create postinstall script
POSTINSTALL_SCRIPT="${SCRIPT_DIR}/postinstall"
cat > "${POSTINSTALL_SCRIPT}" << 'EOF'
#!/bin/bash
# Post-installation script for SharpCoreDB Server

INSTALL_DIR="/usr/local/opt/sharpcoredb"
SERVICE_USER="_sharpcoredb"

# Set permissions
chown -R "${SERVICE_USER}:staff" "${INSTALL_DIR}"
chmod 750 "${INSTALL_DIR}"
chmod 700 "${INSTALL_DIR}/secrets" "${INSTALL_DIR}/certs"

# Load the launchd service
launchctl load "/Library/LaunchDaemons/com.sharpcoredb.server.plist"

echo "SharpCoreDB Server installed successfully!"
echo ""
echo "Next steps:"
echo "1. Configure TLS certificate: ${INSTALL_DIR}/certs/server.pfx"
echo "2. Set JWT secret in configuration"
echo "3. Start service: sudo launchctl load /Library/LaunchDaemons/com.sharpcoredb.server.plist"
echo ""
echo "Service will be available at:"
echo "  gRPC: https://localhost:5001"
echo "  REST: https://localhost:8443"

exit 0
EOF
chmod +x "${POSTINSTALL_SCRIPT}"

# Build the component package
echo "• Building component package..."
pkgbuild \
    --root "${PKG_ROOT}" \
    --identifier "${IDENTIFIER}" \
    --version "1.5.0" \
    --scripts "${SCRIPT_DIR}" \
    --install-location "/" \
    "${SCRIPT_DIR}/${PKG_NAME}-component.pkg"

# Build the distribution package
echo "• Building distribution package..."
cat > "${SCRIPT_DIR}/distribution.xml" << EOF
<?xml version="1.0" encoding="utf-8"?>
<installer-gui-script minSpecVersion="1">
    <title>SharpCoreDB Server</title>
    <organization>com.sharpcoredb</organization>
    <domains enable_anywhere="true"/>
    <options customize="never" require-scripts="true" rootVolumeOnly="false"/>
    <welcome file="welcome.html"/>
    <conclusion file="conclusion.html"/>
    <license file="license.html"/>
    <pkg-ref id="${IDENTIFIER}"/>
    <pkg-ref id="${IDENTIFIER}" version="1.5.0" onConclusion="none">${PKG_NAME}-component.pkg</pkg-ref>
</installer-gui-script>
EOF

# Create welcome/conclusion/license files
cat > "${SCRIPT_DIR}/welcome.html" << 'EOF'
<html>
<head><title>SharpCoreDB Server Installation</title></head>
<body>
<h1>SharpCoreDB Server v1.5.0</h1>
<p>Welcome to the SharpCoreDB Server installer for macOS.</p>
<p>This installer will set up SharpCoreDB Server as a launchd service.</p>
</body>
</html>
EOF

cat > "${SCRIPT_DIR}/conclusion.html" << 'EOF'
<html>
<head><title>Installation Complete</title></head>
<body>
<h1>Installation Complete</h1>
<p>SharpCoreDB Server has been successfully installed.</p>
<p>The service is configured to start automatically on system boot.</p>
<p>Please configure your TLS certificates and JWT secrets before starting the service.</p>
</body>
</html>
EOF

cat > "${SCRIPT_DIR}/license.html" << 'EOF'
<html>
<head><title>License</title></head>
<body>
<h1>MIT License</h1>
<p>Copyright (c) 2026 MPCoreDeveloper</p>
<p>Permission is hereby granted, free of charge, to any person obtaining a copy...</p>
</body>
</html>
EOF

# Build final .pkg
echo "• Creating final .pkg installer..."
productbuild \
    --distribution "${SCRIPT_DIR}/distribution.xml" \
    --package-path "${SCRIPT_DIR}" \
    "${SCRIPT_DIR}/${PKG_FILE}"

# Cleanup
echo "• Cleaning up temporary files..."
rm -rf "${PKG_ROOT}"
rm -f "${SCRIPT_DIR}/${PKG_NAME}-component.pkg"
rm -f "${PREINSTALL_SCRIPT}" "${POSTINSTALL_SCRIPT}"
rm -f "${SCRIPT_DIR}/distribution.xml"
rm -f "${SCRIPT_DIR}/welcome.html" "${SCRIPT_DIR}/conclusion.html" "${SCRIPT_DIR}/license.html"

echo ""
echo "═══════════════════════════════════════════"
echo "  .pkg installer created: ${SCRIPT_DIR}/${PKG_FILE}"
echo ""
echo "  To install: sudo installer -pkg ${PKG_FILE} -target /"
echo "═══════════════════════════════════════════"</content>
<parameter name="filePath">installers/macos/build_pkg.sh
