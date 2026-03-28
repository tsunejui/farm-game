# Open interactive SQLite shell
db:
	#!/bin/sh
	. .env.local 2>/dev/null || just env
	. .env.local
	mise exec -- litecli "$DB_PATH"

# Show all tables
db-tables:
	#!/bin/sh
	. .env.local 2>/dev/null || just env
	. .env.local
	mise exec -- litecli "$DB_PATH" -e ".tables"

# Show schema for a specific table
db-schema table:
	#!/bin/sh
	. .env.local 2>/dev/null || just env
	. .env.local
	mise exec -- litecli "$DB_PATH" -e ".schema {{table}}"

# Run a SQL query
db-query sql:
	#!/bin/sh
	. .env.local 2>/dev/null || just env
	. .env.local
	mise exec -- litecli "$DB_PATH" -e "{{sql}}"

# Show rows from a table (usage: just db-show <table> [limit])
db-show table limit="10":
	#!/bin/sh
	. .env.local 2>/dev/null || just env
	. .env.local
	mise exec -- litecli "$DB_PATH" -e "SELECT * FROM {{table}} LIMIT {{limit}};"

# Backup the database
db-backup:
	#!/bin/sh
	. .env.local 2>/dev/null || just env
	. .env.local
	db_dir=$(dirname "$DB_PATH")
	backup_dir="$db_dir/backups"
	mkdir -p "$backup_dir"
	timestamp=$(date -u '+%Y%m%d_%H%M%S')
	backup_file="$backup_dir/game_${timestamp}.db"
	if [ -f "$DB_PATH" ]; then \
		cp "$DB_PATH" "$backup_file"; \
		echo "Backed up to: $backup_file"; \
	else \
		echo "Database not found. Run 'just start' first."; \
		exit 1; \
	fi

# List all backups
db-backup-list:
	#!/bin/sh
	. .env.local 2>/dev/null || just env
	. .env.local
	backup_dir="$(dirname "$DB_PATH")/backups"
	if [ -d "$backup_dir" ]; then \
		echo "Backups in: $backup_dir"; \
		echo "---"; \
		ls -lh "$backup_dir"/game_*.db 2>/dev/null | awk '{print NR")", $NF, "(" $5 ")"}' || echo "No backups found."; \
	else \
		echo "No backups directory found."; \
	fi

# Restore database from a backup (interactive selector)
db-restore:
	#!/bin/sh
	. .env.local 2>/dev/null || just env
	. .env.local
	backup_dir="$(dirname "$DB_PATH")/backups"
	if [ ! -d "$backup_dir" ] || [ -z "$(ls "$backup_dir"/game_*.db 2>/dev/null)" ]; then \
		echo "No backups found. Run 'just db-backup' first."; \
		exit 1; \
	fi
	selected=$(ls -1t "$backup_dir"/game_*.db | xargs -I{} basename {} | gum choose --header "Select a backup to restore:")
	if [ -z "$selected" ]; then \
		echo "Cancelled."; \
		exit 0; \
	fi
	gum confirm "Restore from $selected?" || exit 0
	cp "$DB_PATH" "$DB_PATH.before_restore" 2>/dev/null
	cp "$backup_dir/$selected" "$DB_PATH"
	echo "Restored from: $selected"
	echo "Previous DB saved as: $(basename "$DB_PATH").before_restore"

# Show database file path and size
db-info:
	#!/bin/sh
	. .env.local 2>/dev/null || just env
	. .env.local
	echo "Path: $DB_PATH"
	if [ -f "$DB_PATH" ]; then \
		echo "Size: $(du -h "$DB_PATH" | cut -f1)"; \
		mise exec -- litecli "$DB_PATH" -e "SELECT version, applied_at, description FROM schema_version ORDER BY version;"; \
	else \
		echo "Database not found. Run 'just start' first."; \
	fi
