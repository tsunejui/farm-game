// =============================================================================
// DatabaseManager.cs — Database infrastructure management
//
// Handles connection lifecycle, schema migration, backup, and path resolution.
// CRUD operations live in the Repository classes under Persistence/Repositories/.
//
// Usage:
//   var dbManager = new DatabaseManager(gameName);
//   dbManager.Initialize();               // Dir check, create tables, migrate, backup
//   using var conn = dbManager.Connect(); // Create a connection for repository use
//   dbManager.Backup();                   // Manual backup
// =============================================================================

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FarmGame.Persistence.Entities;
using Serilog;
using SQLite;

namespace FarmGame.Persistence;

public class DatabaseManager
{
    private readonly string _databasePath;
    private readonly string _databaseDir;

    private const string DatabaseFileName = "game.db";
    private const string BackupDirectory = "backups";
    private const int MaxBackups = 5;
    private const long MinFreeSpaceBytes = 10 * 1024 * 1024; // 10 MB
    public const int CurrentSchemaVersion = 2;

    public string DatabasePath => _databasePath;

    public DatabaseManager(string gameName)
    {
        // Check env for explicit DB path first
        var envDbPath = System.Environment.GetEnvironmentVariable("DB_PATH");
        if (!string.IsNullOrEmpty(envDbPath))
        {
            _databasePath = envDbPath;
            _databaseDir = Path.GetDirectoryName(envDbPath);
        }
        else
        {
            _databaseDir = ResolveDatabaseDirectory(gameName);
            _databasePath = Path.Combine(_databaseDir, DatabaseFileName);
        }
    }

    // ─── Initialization ─────────────────────────────────────

    /// <summary>
    /// Full initialization: ensure directory, check permissions, create tables,
    /// run schema migrations, and create a backup.
    /// </summary>
    public DatabaseResult Initialize()
    {
        var dirResult = EnsureDirectoryExists();
        if (!dirResult.Success) return dirResult;

        var permResult = CheckWritePermission();
        if (!permResult.Success) return permResult;

        var spaceResult = CheckDiskSpace();
        if (!spaceResult.Success) return spaceResult;

        try
        {
            using var db = Connect();
            db.CreateTable<SchemaVersion>();
            db.CreateTable<Setting>();
            db.CreateTable<PlayerStateRecord>();
            db.CreateTable<MapStateRecord>();
            db.CreateTable<MapObjectRecord>();
            db.CreateTable<ObjectEffectRecord>();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to initialize database: {ex.Message}");
        }

        var migrationResult = Migrate();
        if (!migrationResult.Success) return migrationResult;

        Backup();

        Log.Information("[DatabaseManager] Initialized: {Path}", _databasePath);
        return DatabaseResult.Ok();
    }

    // ─── Connection ─────────────────────────────────────────

    /// <summary>
    /// Create a new SQLite connection. Caller is responsible for disposal.
    /// Used by repositories for each operation.
    /// </summary>
    public SQLiteConnection Connect() => new(_databasePath);

    // ─── Backup ─────────────────────────────────────────────

    public DatabaseResult Backup()
    {
        if (!File.Exists(_databasePath))
            return DatabaseResult.Ok();

        try
        {
            var backupDir = Path.Combine(_databaseDir, BackupDirectory);
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupDir, $"game_{timestamp}.db");
            File.Copy(_databasePath, backupPath, overwrite: true);

            var files = new DirectoryInfo(backupDir).GetFiles("game_*.db");
            if (files.Length > MaxBackups)
            {
                Array.Sort(files, (a, b) => a.CreationTimeUtc.CompareTo(b.CreationTimeUtc));
                for (int i = 0; i < files.Length - MaxBackups; i++)
                    files[i].Delete();
            }

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Backup failed: {ex.Message}");
        }
    }

    // ─── Schema Migration ───────────────────────────────────

    private DatabaseResult Migrate()
    {
        try
        {
            using var db = Connect();
            int dbVersion = GetCurrentDbVersion(db);

            for (int version = dbVersion + 1; version <= CurrentSchemaVersion; version++)
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
                            // Column may already exist if table was created with the entity class
                            try { db.Execute("ALTER TABLE map_state ADD COLUMN TtlUtc INTEGER NOT NULL DEFAULT 0"); }
                            catch { /* column already exists */ }
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
            }

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.MigrationFailed,
                $"Schema migration failed: {ex.Message}");
        }
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

    // ─── Path Resolution ────────────────────────────────────

    public static string ResolveDatabaseDirectory(string gameName)
    {
        string safeName = SanitizeName(gameName);
        string baseDir;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        else
        {
            baseDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrEmpty(baseDir))
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "share");
        }

        return Path.Combine(baseDir, safeName);
    }

    // ─── Filesystem Checks ──────────────────────────────────

    private DatabaseResult EnsureDirectoryExists()
    {
        try
        {
            Directory.CreateDirectory(_databaseDir);
            return DatabaseResult.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.PermissionDenied,
                $"Permission denied: cannot create directory '{_databaseDir}'.");
        }
        catch (IOException ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.DirectoryCreationFailed,
                $"Failed to create directory '{_databaseDir}': {ex.Message}");
        }
    }

    private DatabaseResult CheckWritePermission()
    {
        string testFile = Path.Combine(_databaseDir, ".write_test");
        try
        {
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return DatabaseResult.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.PermissionDenied,
                $"Permission denied: cannot write to '{_databaseDir}'.");
        }
        catch (IOException ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.PermissionDenied,
                $"Cannot write to '{_databaseDir}': {ex.Message}");
        }
    }

    private DatabaseResult CheckDiskSpace()
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_databaseDir)));
            if (driveInfo.AvailableFreeSpace < MinFreeSpaceBytes)
                return DatabaseResult.Fail(DatabaseErrorKind.DiskSpaceInsufficient,
                    "Insufficient disk space. At least 10 MB of free space is required.");
        }
        catch
        {
            // DriveInfo may not work on all platforms — proceed anyway
        }

        return DatabaseResult.Ok();
    }

    private static string SanitizeName(string name)
    {
        string sanitized = name.Replace(' ', '_');
        sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9_]", "");
        return string.IsNullOrEmpty(sanitized) ? "FarmGame" : sanitized;
    }
}
