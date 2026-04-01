using System;
using System.Linq;
using SQLite;
using FarmGame.Persistence;
using FarmGame.Persistence.Entities;

namespace FarmGame.Core;

public static class MigrationManager
{
    public const int CurrentSchemaVersion = 2;

    public static DatabaseResult Migrate(SQLiteConnection db)
    {
        int dbVersion = GetCurrentDbVersion(db);

        for (int version = dbVersion + 1; version <= CurrentSchemaVersion; version++)
        {
            var result = ApplyMigration(db, version);
            if (!result.Success)
                return result;
        }

        return DatabaseResult.Ok();
    }

    private static int GetCurrentDbVersion(SQLiteConnection db)
    {
        try
        {
            var versions = db.Table<SchemaVersion>().ToList();
            return versions.Count == 0 ? 0 : versions.Max(v => v.Version);
        }
        catch
        {
            return 0;
        }
    }

    private static DatabaseResult ApplyMigration(SQLiteConnection db, int version)
    {
        try
        {
            db.RunInTransaction(() =>
            {
                switch (version)
                {
                    case 1:
                        db.Insert(new SchemaVersion
                        {
                            Version = 1,
                            AppliedAt = DateTime.UtcNow.ToString("o"),
                            Description = "Initial schema"
                        });
                        break;

                    case 2:
                        db.Execute("ALTER TABLE map_state ADD COLUMN TtlUtc INTEGER NOT NULL DEFAULT 0");
                        db.Insert(new SchemaVersion
                        {
                            Version = 2,
                            AppliedAt = DateTime.UtcNow.ToString("o"),
                            Description = "Add TtlUtc column to map_state"
                        });
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown migration version: {version}");
                }
            });

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.MigrationFailed,
                $"Migration to version {version} failed: {ex.Message}");
        }
    }
}
