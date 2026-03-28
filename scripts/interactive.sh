#!/bin/bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if ! command -v gum >/dev/null 2>&1; then
  echo "gum is required to run this interactive selector" >&2
  echo "Install with: mise install" >&2
  exit 1
fi

# Parse --back flag
BACK_CMD=""
if [ "${1:-}" = "--back" ]; then
  BACK_CMD="$2"
  shift 2
fi

if [ "$#" -eq 0 ]; then
  echo "At least one option must be provided in the format label::command" >&2
  exit 1
fi

declare -a LABELS
declare -a COMMANDS

# Prepend back option if set
if [ -n "$BACK_CMD" ]; then
  LABELS+=("← Back")
  COMMANDS+=("$BACK_CMD")
fi

for option in "$@"; do
  if [[ "$option" != *"::"* ]]; then
    echo "Invalid option '$option'. Use the format label::command" >&2
    exit 1
  fi
  label="${option%%::*}"
  command="${option#*::}"
  if [ -z "$label" ] || [ -z "$command" ] || [ "$label" = "$command" ]; then
    echo "Invalid option '$option'. Labels and commands must be non-empty" >&2
    exit 1
  fi
  LABELS+=("$label")
  COMMANDS+=("$command")
done

header="${INTERACTIVE_HEADER:-Select an action}"

# Handle ESC (gum exits non-zero) → run back command or exit
if ! selection="$(gum choose --header "$header" "${LABELS[@]}")"; then
  if [ -n "$BACK_CMD" ]; then
    cd "$ROOT_DIR"
    eval "$BACK_CMD"
    exit $?
  fi
  exit 0
fi

for idx in "${!LABELS[@]}"; do
  if [ "${LABELS[$idx]}" = "$selection" ]; then
    cd "$ROOT_DIR"
    eval "${COMMANDS[$idx]}"
    exit $?
  fi
done

echo "Selection '$selection' not found" >&2
exit 1
