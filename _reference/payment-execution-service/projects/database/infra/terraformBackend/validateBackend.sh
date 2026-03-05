#!/usr/bin/env bash

set -euo pipefail

CUR_DIR=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
STACK_PATH="$CUR_DIR/terraformBackendStack.yaml"

aws cloudformation validate-template \
    --template-body file://"$STACK_PATH"