#!/bin/bash
# SharpCoreDB Benchmark Runner (Bash)
# Runs all benchmark scenarios and generates reports

set -e

# Configuration
CONFIGURATION="${1:-Release}"
OUTPUT_DIR="results"
SKIP_BUILD="${SKIP_BUILD:-false}"

echo "========================================"
echo "  SharpCoreDB Benchmark Suite"
echo "========================================"
echo ""

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_DIR="$SCRIPT_DIR/SharpCoreDB.Benchmarks"
OUTPUT_PATH="$SCRIPT_DIR/$OUTPUT_DIR"
TIMESTAMP=$(date +"%Y-%m-%d-%H%M%S")
RUN_OUTPUT_DIR="$OUTPUT_PATH/$TIMESTAMP"

echo "[Runner] Configuration: $CONFIGURATION"
echo "[Runner] Output directory: $RUN_OUTPUT_DIR"
echo ""

# Build project
if [ "$SKIP_BUILD" != "true" ]; then
    echo "[Runner] Building project..."
    cd "$PROJECT_DIR"
    dotnet build --configuration "$CONFIGURATION"
    
    if [ $? -ne 0 ]; then
        echo "[Runner] Build failed!"
        exit 1
    fi
    
    cd "$SCRIPT_DIR"
    echo "[Runner] Build successful!"
    echo ""
else
    echo "[Runner] Skipping build (SKIP_BUILD=true)"
    echo ""
fi

# Create output directory
mkdir -p "$RUN_OUTPUT_DIR"
echo "[Runner] Created output directory: $RUN_OUTPUT_DIR"
echo ""

# Run benchmarks
echo "[Runner] Starting benchmark execution..."
echo ""

cd "$PROJECT_DIR"
START_TIME=$(date +%s)

# Execute benchmark
dotnet run --configuration "$CONFIGURATION" --no-build

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

cd "$SCRIPT_DIR"

echo ""
echo "========================================"
echo "  Benchmark Run Complete"
echo "========================================"
echo "Duration: $(($DURATION / 60)) minutes $(($DURATION % 60)) seconds"
echo "Results saved to: $RUN_OUTPUT_DIR"
echo ""

# List generated files
echo "Generated files:"
find "$RUN_OUTPUT_DIR" -type f | while read -r file; do
    echo "  - $(basename "$file")"
done

echo ""
echo "[Runner] Done!"
