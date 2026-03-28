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
