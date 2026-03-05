#!/usr/bin/env bash

set -e

# Component Tests Coverage Script
# Runs component tests with code coverage using .runsettings configuration
#
# Prerequisites:
#   Docker dependencies must be running before executing this script:
#   - PostgreSQL database (port 5432)
#   - Identity mock (port 5003)
#   - Stripe execution mock (port 12112)
#
#   Start dependencies with: make start-local-dependencies
#   Or run: make component-test (automatically starts/stops dependencies)

echo "======================================"
echo "Running Cancel Lambda Component Tests"
echo "======================================"
echo ""

# Set working directory to the lambda root
cd "$(dirname "$0")/.."

# Test project path
TEST_PROJECT="tests/PaymentExecutionLambda.CancelLambda.ComponentTests"
RESULTS_DIR="tests/results"

# Clean previous results
echo "Cleaning previous test results..."
rm -rf "$RESULTS_DIR"
mkdir -p "$RESULTS_DIR"

# Run component tests with coverage
echo "Running component tests with coverage..."
dotnet test "$TEST_PROJECT" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$RESULTS_DIR" \
  --settings "$TEST_PROJECT/.runsettings" \
  --logger "trx;LogFileName=component-test-results.trx" \
  --verbosity normal

echo ""
echo "======================================"
echo "Component Tests Complete!"
echo "======================================"
echo ""
echo "Results:"
echo "  - Test Results: $RESULTS_DIR/**/component-test-results.trx"
echo "  - Coverage: $RESULTS_DIR/**/coverage.opencover.xml"
echo ""
