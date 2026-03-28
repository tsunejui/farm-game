# Generate local environment config (.env.local)
env:
	#!/bin/sh
	case "$(uname)" in \
		Darwin) db_dir="$HOME/Library/Application Support/Farm_Game" ;; \
		Linux) db_dir="${XDG_DATA_HOME:-$HOME/.local/share}/Farm_Game" ;; \
		MINGW*|MSYS*|CYGWIN*) db_dir="$LOCALAPPDATA/Farm_Game" ;; \
		*) db_dir="$HOME/.local/share/Farm_Game" ;; \
	esac; \
	cat > .env.local <<EOF
	# Auto-generated local environment config — do not commit
	# Generated at: $(date -u '+%Y-%m-%dT%H:%M:%SZ')
	OS=$(uname)
	GAME_TITLE=Farm Game
	DB_PATH=$db_dir/game.db
	EOF
	echo "Generated .env.local"
