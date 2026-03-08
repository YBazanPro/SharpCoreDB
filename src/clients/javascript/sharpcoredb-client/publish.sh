#!/usr/bin/env bash
# @sharpcoredb/client - Build and publish to npm
# Usage: bash publish.sh

set -euo pipefail

echo "@sharpcoredb/client - npm Publishing Script"
echo "==========================================="

# Check if we're in the right directory
if [[ ! -f "package.json" ]]; then
    echo "ERROR: Run this script from the sharpcoredb-client directory"
    exit 1
fi

# Check for required tools
if ! command -v node &> /dev/null; then
    echo "ERROR: Node.js not found"
    exit 1
fi

if ! command -v npm &> /dev/null; then
    echo "ERROR: npm not found"
    exit 1
fi

# Clean previous builds
echo "• Cleaning previous builds..."
rm -rf dist/

# Install dependencies
echo "• Installing dependencies..."
npm ci

# Run tests
echo "• Running tests..."
npm test

# Type checking
echo "• Running type check..."
npm run typecheck

# Build the package
echo "• Building package..."
npm run build

# Verify the build
echo "• Verifying build..."
if [[ ! -f "dist/index.js" ]]; then
    echo "ERROR: Build failed - dist/index.js not found"
    exit 1
fi

if [[ ! -f "dist/index.d.ts" ]]; then
    echo "ERROR: Build failed - dist/index.d.ts not found"
    exit 1
fi

# Show what was built
echo "• Build artifacts:"
ls -la dist/

echo ""
echo "📦 Package built successfully!"
echo ""
echo "To publish to npm:"
echo "  1. Test on npm beta tag first:"
echo "     npm publish --tag beta"
echo "  2. Install from beta:"
echo "     npm install @sharpcoredb/client@beta"
echo "  3. If tests pass, publish to latest:"
echo "     npm publish"
echo ""
echo "Note: You'll need npm credentials configured or use 'npm login' first"
echo "For automated publishing, consider GitHub Actions with npm auth"
