# Generate local environment config (.env.local)
env:
	#!/bin/sh
	case "$(uname)" in \
		Darwin) db_dir="$HOME/Library/Application Support/Farm_Game" ;; \
		Linux) db_dir="${XDG_DATA_HOME:-$HOME/.local/share}/Farm_Game" ;; \
		MINGW*|MSYS*|CYGWIN*) db_dir="$LOCALAPPDATA/Farm_Game" ;; \
		*) db_dir="$HOME/.local/share/Farm_Game" ;; \
	esac
	project_root="$(cd "$(dirname "$0")" && pwd)"
	printf '%s\n' \
		"# Auto-generated local environment config — do not commit" \
		"# Generated at: $(date -u '+%Y-%m-%dT%H:%M:%SZ')" \
		"GAME_TITLE=\"Farm Game\"" \
		"DB_PATH=\"$db_dir/game.db\"" \
		"LOG_DIR=\"$db_dir/logs\"" \
		"CONFIGS_DIR=\"$project_root/configs\"" \
		> .env.local
	echo "Generated .env.local"
