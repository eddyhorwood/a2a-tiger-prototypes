#!/usr/bin/env bash

set -euo pipefail

array=()
array+=("GNU Make=make --version")

DARK_GRAY='\033[0;30m'
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

IS_SET_UP=true

test_tool() {
  set +e
  CMD_OUTPUT=$($2 2>&1)
  EXIT_CODE=$?
  set -e

  if [ -z "$CMD_OUTPUT" ] || [ "$EXIT_CODE" != "0" ]; then
    echo -e "${RED}$1 is not installed${NC}"
    IS_SET_UP=false

  else
    echo "$1 is correctly installed"
    return
  fi
}

check_tool_version() {
  TOOL_NAME="$1"
  VERSION_COMMAND="$2"
  SED_COMMAND="$3"
  REQUIRED_VER="$4"

  set +e
  TOOL_OUTPUT=$($VERSION_COMMAND 2>&1)
  EXIT_CODE=$?
  set -e

  if [ -z "$TOOL_OUTPUT" ] || [ "$EXIT_CODE" != "0" ]; then
    echo -e "${RED}$TOOL_NAME is not installed${NC}"
    IS_SET_UP=false
    return
  fi

  TOOL_VERSION=$(echo "$TOOL_OUTPUT" | sed "$SED_COMMAND")
  LOWEST_VER=$(echo -e "$TOOL_VERSION\n$REQUIRED_VER" | sort -V | head -1)

  if [[ "$LOWEST_VER" == "$REQUIRED_VER" ]]; then
    echo "$TOOL_NAME version $TOOL_VERSION is correctly installed (min version = $REQUIRED_VER)"
  else
    echo -e "${RED}$TOOL_NAME version $TOOL_VERSION is outdated (min version = $REQUIRED_VER)${NC}"
    IS_SET_UP=false
  fi
}

setup_git_hooks() {
  echo -e "${DARK_GRAY}Setting up Git hooks...${NC}"
  git config core.hooksPath ./scripts/hooks
}

echo -e "${DARK_GRAY}Checking prerequisites...${NC}"
for element in "${array[@]}"; do
  name="${element%%=*}"
  command="${element#*=}"
  test_tool "$name" "$command"
done

check_tool_version "Docker" "docker --version" 's/Docker version //;s/,.*//' "19.03.7"
check_tool_version ".NET SDK" "dotnet --version" 's/^ *//;s/ *$//' "8.0.100"
check_tool_version "Docker Compose" "docker compose version" 's/Docker Compose version v//;s/,.*//' "2.1.1"

setup_git_hooks

if [[ $IS_SET_UP == false ]]; then
  echo -e "${RED}Some prerequisites aren't installed correctly. Please check the docs/local-setup.md for guidance.${NC}"
  exit 1
else
  echo -e "${GREEN}Your local environment is correctly set up${NC}"
fi
