# Generate local environment config (.env.local)
env:
	#!/bin/sh
	project_root="$(cd "$(dirname "$0")" && pwd)"
	data_dir="$project_root/.data"
	printf '%s\n' \
		"# Auto-generated local environment config — do not commit" \
		"# Generated at: $(date -u '+%Y-%m-%dT%H:%M:%SZ')" \
		"GAME_TITLE=\"Farm Game\"" \
		"DB_PATH=\"$data_dir/game.db\"" \
		"LOG_DIR=\"$data_dir/logs\"" \
		"CONFIGS_DIR=\"$project_root/configs\"" \
		"CONTENT_DIR=\"$project_root/game/FarmGame/Content\"" \
		> .env.local
	echo "Generated .env.local"
