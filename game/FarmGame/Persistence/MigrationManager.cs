using System;
using Microsoft.Data.Sqlite;

namespace FarmGame.Persistence;

public static class MigrationManager
{
    public const int CurrentSchemaVersion = 2;

    public static DatabaseResult Migrate(SqliteConnection connection)
    {
        int dbVersion = GetCurrentDbVersion(connection);

        for (int version = dbVersion + 1; version <= CurrentSchemaVersion; version++)
        {
            var result = ApplyMigration(connection, version);
            if (!result.Success)
                return result;
        }

        return DatabaseResult.Ok();
    }

    private static int GetCurrentDbVersion(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT MAX(version) FROM schema_version;";
        try
        {
            var result = cmd.ExecuteScalar();
            return result is DBNull or null ? 0 : Convert.ToInt32(result);
        }
        catch (SqliteException)
        {
            // Table doesn't exist yet
            return 0;
        }
    }

    private static DatabaseResult ApplyMigration(SqliteConnection connection, int version)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;

            switch (version)
            {
                case 1:
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS schema_version (
                            version INTEGER NOT NULL,
                            applied_at TEXT NOT NULL DEFAULT (datetime('now')),
                            description TEXT
                        );

                        CREATE TABLE IF NOT EXISTS save_metadata (
                            id INTEGER PRIMARY KEY,
                            slot_name TEXT NOT NULL,
                            created_at TEXT NOT NULL DEFAULT (datetime('now')),
                            updated_at TEXT NOT NULL DEFAULT (datetime('now')),
                            play_time_seconds INTEGER NOT NULL DEFAULT 0,
                            game_version TEXT NOT NULL
                        );

                        INSERT INTO schema_version (version, description)
                        VALUES (1, 'Initial schema');
                    ";
                    break;

                case 2:
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS player_state (
                            id INTEGER PRIMARY KEY,
                            slot_name TEXT NOT NULL UNIQUE,
                            state_json TEXT NOT NULL,
                            game_version TEXT NOT NULL,
                            created_at TEXT NOT NULL DEFAULT (datetime('now')),
                            updated_at TEXT NOT NULL DEFAULT (datetime('now'))
                        );

                        INSERT INTO schema_version (version, description)
                        VALUES (2, 'Add player_state table');
                    ";
                    break;

                default:
                    return DatabaseResult.Fail(DatabaseErrorKind.MigrationFailed,
                        $"Unknown migration version: {version}");
            }

            cmd.ExecuteNonQuery();
            transaction.Commit();
            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return DatabaseResult.Fail(DatabaseErrorKind.MigrationFailed,
                $"Migration to version {version} failed: {ex.Message}");
        }
    }
}
