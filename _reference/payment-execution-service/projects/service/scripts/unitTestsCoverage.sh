#!/usr/bin/env bash

set -euo pipefail

CUR_DIR=$(dirname "$(readlink -f "$0")")
TEST_RESULT_PATH="$CUR_DIR/../tests/results/unit-tests"
PROJECT_PATH="$CUR_DIR/../tests/PaymentExecutionService.UnitTests"

dotnet test "$PROJECT_PATH" \
  --nologo \
  --logger:"trx;verbosity=normal;logfilename=$TEST_RESULT_PATH/TestResults/UnitTestResults.trx" \
  --logger:"console;verbosity=normal" \
  -c Release \
  --collect:"XPlat Code Coverage" \
  --results-directory="$TEST_RESULT_PATH/opencover/generated" \

mv -f $TEST_RESULT_PATH/opencover/generated/**/coverage.opencover.xml $TEST_RESULT_PATH/opencover/
rm -rf $TEST_RESULT_PATH/opencover/generated

reportgenerator \
  "-reports:$TEST_RESULT_PATH/opencover/coverage.opencover.xml" \
  "-targetdir:$TEST_RESULT_PATH/coverage-report" \
  "-reporttypes:Html"
