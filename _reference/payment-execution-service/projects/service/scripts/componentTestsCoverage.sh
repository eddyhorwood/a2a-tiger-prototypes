#!/usr/bin/env bash

set -euo pipefail

export DOTNET_ENVIRONMENT=$1
[[ -z "$DOTNET_ENVIRONMENT" ]] && echo "DOTNET_ENVIRONMENT empty" && exit 1

if [ "$DOTNET_ENVIRONMENT" == "Development" ]; then
  export Override_Identity__Authority="http://identity-mock:80"
  export Override_Authorisation__AuthorisationServiceHost="http://auth-service:50051"
fi

CUR_DIR=$(dirname "$(readlink -f "$0")")
TEST_RESULT_PATH="$CUR_DIR/../tests/results/component-tests"
PROJECT_PATH="$CUR_DIR/../tests/PaymentExecutionService.ComponentTests"

dotnet test "$PROJECT_PATH" \
  --nologo \
  --logger:"trx;verbosity=normal;logfilename=$TEST_RESULT_PATH/TestResults/ComponentTestResults.trx" \
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
