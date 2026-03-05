#!/usr/bin/env bash

set -euo pipefail

CUR_DIR=$(dirname "$(readlink -f "$0")")
TEST_RESULT_PATH="$CUR_DIR/../tests/results/unit-tests"
PROJECT_PATH="$CUR_DIR/../tests/PaymentExecutionLambda.CancelLambda.UnitTests"

dotnet test "$PROJECT_PATH" \
  --nologo \
  --logger:"trx;verbosity=normal;logfilename=$TEST_RESULT_PATH/TestResults/UnitTestResults.trx" \
  --logger:"console;verbosity=normal" \
  -c Release \
  --collect:"XPlat Code Coverage" \
  --results-directory="$TEST_RESULT_PATH/coverage/generated" \

# Find and move the coverage file
find "$TEST_RESULT_PATH/coverage/generated" -name "coverage.cobertura.xml" -exec mv {} "$TEST_RESULT_PATH/coverage/coverage.cobertura.xml" \; -quit
rm -rf "$TEST_RESULT_PATH/coverage/generated"

# Generate HTML coverage report if reportgenerator is available
if command -v reportgenerator &> /dev/null; then
  reportgenerator \
    "-reports:$TEST_RESULT_PATH/coverage/coverage.cobertura.xml" \
    "-targetdir:$TEST_RESULT_PATH/coverage-report" \
    "-reporttypes:Html"
  echo "Coverage report generated at: $TEST_RESULT_PATH/coverage-report/index.html"
else
  echo "reportgenerator not found. Skipping HTML report generation."
  echo "Install with: dotnet tool install -g dotnet-reportgenerator-globaltool"
  echo "Coverage data available at: $TEST_RESULT_PATH/coverage/coverage.cobertura.xml"
fi

