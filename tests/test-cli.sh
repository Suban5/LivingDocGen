#!/bin/bash

# Test script for LivingDocGen CLI
# This script tests various CLI commands and validates outputs
# run script using: ./test-cli.sh

set -e  # Exit on error

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLI_PROJECT="$SCRIPT_DIR/../src/LivingDocGen.CLI/LivingDocGen.CLI.csproj"
TEST_DIR="$SCRIPT_DIR/IntegrationTest.Net7"
REPORT_DIR="$TEST_DIR/TestResults/xml"
OUTPUT_DIR="$SCRIPT_DIR/cli-test-output"

echo "=========================================="
echo "  LivingDocGen CLI Test Suite"
echo "=========================================="
echo ""

# Clean up previous test outputs
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Counter for tests
TESTS_RUN=0
TESTS_PASSED=0
TESTS_FAILED=0

# Function to run a test
run_test() {
    local test_name="$1"
    local command="$2"
    local expected_pattern="$3"
    
    TESTS_RUN=$((TESTS_RUN + 1))
    echo -n "[$TESTS_RUN] Testing: $test_name... "
    
    # Run the command and capture output
    if OUTPUT=$(eval "$command" 2>&1); then
        if [[ -z "$expected_pattern" ]] || echo "$OUTPUT" | grep -q "$expected_pattern"; then
            echo -e "${GREEN}✓ PASSED${NC}"
            TESTS_PASSED=$((TESTS_PASSED + 1))
            return 0
        else
            echo -e "${RED}✗ FAILED${NC}"
            echo "  Expected pattern not found: $expected_pattern"
            echo "  Got: $OUTPUT"
            TESTS_FAILED=$((TESTS_FAILED + 1))
            return 1
        fi
    else
        echo -e "${RED}✗ FAILED${NC}"
        echo "  Command failed with exit code $?"
        echo "  Output: $OUTPUT"
        TESTS_FAILED=$((TESTS_FAILED + 1))
        return 1
    fi
}

# Function to check if file exists
check_file() {
    local test_name="$1"
    local file_path="$2"
    
    TESTS_RUN=$((TESTS_RUN + 1))
    echo -n "[$TESTS_RUN] Checking: $test_name... "
    
    if [[ -f "$file_path" ]]; then
        echo -e "${GREEN}✓ PASSED${NC}"
        TESTS_PASSED=$((TESTS_PASSED + 1))
        return 0
    else
        echo -e "${RED}✗ FAILED${NC}"
        echo "  File not found: $file_path"
        TESTS_FAILED=$((TESTS_FAILED + 1))
        return 1
    fi
}

echo "Building CLI project..."
dotnet build "$CLI_PROJECT" --verbosity quiet > /dev/null 2>&1
echo -e "${GREEN}✓ Build successful${NC}"
echo ""

# Test 1: Version command
run_test "CLI --version" \
    "dotnet run --project '$CLI_PROJECT' --no-build -- --version" \
    "2.0.0"

# Test 2: Help command
run_test "CLI --help" \
    "dotnet run --project '$CLI_PROJECT' --no-build -- --help" \
    "Universal BDD Living Documentation Generator"

# Test 3: Generate command help
run_test "generate --help" \
    "dotnet run --project '$CLI_PROJECT' --no-build -- generate --help" \
    "Generate living documentation HTML"

# Test 4: Generate with default settings
run_test "generate with defaults" \
    "dotnet run --project '$CLI_PROJECT' --no-build -- generate '$TEST_DIR/Features' '$REPORT_DIR' -o '$OUTPUT_DIR/default.html' > /dev/null 2>&1" \
    ""

check_file "default HTML generated" "$OUTPUT_DIR/default.html"

# Test 5: Generate with custom title
run_test "generate with custom title" \
    "dotnet run --project '$CLI_PROJECT' --no-build -- generate '$TEST_DIR/Features' '$REPORT_DIR' -o '$OUTPUT_DIR/custom-title.html' -t 'Custom Test Title' > /dev/null 2>&1" \
    ""

check_file "custom title HTML generated" "$OUTPUT_DIR/custom-title.html"

# Test 6: Generate with different theme
run_test "generate with blue theme" \
    "dotnet run --project '$CLI_PROJECT' --no-build -- generate '$TEST_DIR/Features' '$REPORT_DIR' -o '$OUTPUT_DIR/blue-theme.html' --theme blue > /dev/null 2>&1" \
    ""

check_file "blue theme HTML generated" "$OUTPUT_DIR/blue-theme.html"

# Test 7: Generate with config file (if exists)
if [[ -f "$TEST_DIR/livingdocgen.json" ]]; then
    run_test "generate with config file" \
        "cd '$TEST_DIR' && dotnet run --project '$CLI_PROJECT' --no-build -- generate Features '$REPORT_DIR' -o '$OUTPUT_DIR/from-config.html' > /dev/null 2>&1" \
        ""
    
    check_file "config-based HTML generated" "$OUTPUT_DIR/from-config.html"
fi

# Test 8: Invalid command
run_test "invalid command error" \
    "dotnet run --project '$CLI_PROJECT' --no-build -- invalidcommand 2>&1 || true" \
    "Required command was not provided"

# Test 9: Invalid features path (CLI uses default ./Features when not provided)
run_test "invalid features path error" \
    "dotnet run --project '$CLI_PROJECT' --no-build -- generate 2>&1 || true" \
    "Invalid features path"

echo ""
echo "=========================================="
echo "  Test Summary"
echo "=========================================="
echo "Total Tests:  $TESTS_RUN"
echo -e "Passed:       ${GREEN}$TESTS_PASSED${NC}"
echo -e "Failed:       ${RED}$TESTS_FAILED${NC}"
echo ""

if [[ $TESTS_FAILED -eq 0 ]]; then
    echo -e "${GREEN}🎉 All tests passed!${NC}"
    echo ""
    echo "Generated test outputs can be found in:"
    echo "  $OUTPUT_DIR"
    exit 0
else
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi
