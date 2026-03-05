#!/usr/bin/env bash

set -euo pipefail

export DOTNET_ENVIRONMENT=$1
[[ -z "$DOTNET_ENVIRONMENT" ]] && echo "DOTNET_ENVIRONMENT empty" && exit 1

if [ "$DOTNET_ENVIRONMENT" == "Development" ]; then
  export Override_Identity__Authority="http://identity-mock:80"
  export Override_Authorisation__AuthorisationServiceHost="http://auth-service:50051"
	export Override_AWS__ServiceURL="http://localstack:4566"
fi

CUR_DIR=$(dirname "$(readlink -f "$0")")
PROJECT_PATH="$CUR_DIR/../tests/PaymentExecutionService.ProviderPactTests"

dotnet test "$PROJECT_PATH" \
  --nologo \
  --logger:"console;verbosity=normal" \
  -c Release

