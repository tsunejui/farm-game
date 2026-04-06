// =============================================================================
// DatabaseManager.cs — Unified database access layer
//
// Consolidates all SQLite operations: connection management, schema migration,
// backup, and CRUD for player state, map state, objects, effects, and settings.
//
// Usage:
//   var db = new DatabaseManager(gameName);
//   db.Initialize();                          // Create tables, run migrations, backup
//   db.SavePlayerState(uuid, state, ver);     // Player CRUD
//   db.LoadPlayerState(uuid);
//   db.GetSetting("language");                // Settings KV store
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FarmGame.Models;
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
        _databaseDir = ResolveDatabaseDirectory(gameName);
        _databasePath = Path.Combine(_databaseDir, DatabaseFileName);
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
            using var db = CreateConnection();
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

        // Run schema migrations
        var migrationResult = Migrate();
        if (!migrationResult.Success) return migrationResult;

        // Backup after successful init
        Backup();

        Log.Information("[DatabaseManager] Initialized: {Path}", _databasePath);
        return DatabaseResult.Ok();
    }

    // ─── Connection ─────────────────────────────────────────

    private SQLiteConnection CreateConnection() => new(_databasePath);

    // ─── Settings ───────────────────────────────────────────

    public string GetSetting(string key, string defaultValue = null)
    {
        try
        {
            using var db = CreateConnection();
            var setting = db.Find<Setting>(key);
            return setting?.Value ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public DatabaseResult SetSetting(string key, string value)
    {
        try
        {
            using var db = CreateConnection();
            db.InsertOrReplace(new Setting { Key = key, Value = value });
            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save setting '{key}': {ex.Message}");
        }
    }

    // ─── Player State ───────────────────────────────────────

    public DatabaseResult SavePlayerState(string playerUuid, PlayerState state, string gameVersion)
    {
        try
        {
            using var db = CreateConnection();
            var existing = db.Table<PlayerStateRecord>()
                .FirstOrDefault(r => r.PlayerUuid == playerUuid);

            var now = DateTime.UtcNow.ToString("o");

            if (existing != null)
            {
                existing.StateJson = state.ToJson();
                existing.GameVersion = gameVersion;
                existing.MaxHp = state.MaxHp;
                existing.CurrentHp = state.CurrentHp;
                existing.Strength = state.Strength;
                existing.Dexterity = state.Dexterity;
                existing.WeaponAtk = state.WeaponAtk;
                existing.BuffPercent = state.BuffPercent;
                existing.CritRate = state.CritRate;
                existing.CritDamage = state.CritDamage;
                existing.UpdatedAt = now;
                db.Update(existing);
            }
            else
            {
                db.Insert(new PlayerStateRecord
                {
                    PlayerUuid = playerUuid,
                    StateJson = state.ToJson(),
                    GameVersion = gameVersion,
                    MaxHp = state.MaxHp,
                    CurrentHp = state.CurrentHp,
                    Strength = state.Strength,
                    Dexterity = state.Dexterity,
                    WeaponAtk = state.WeaponAtk,
                    BuffPercent = state.BuffPercent,
                    CritRate = state.CritRate,
                    CritDamage = state.CritDamage,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save player state: {ex.Message}");
        }
    }

    public DatabaseResult<PlayerState> LoadPlayerState(string playerUuid)
    {
        try
        {
            using var db = CreateConnection();
            var record = db.Table<PlayerStateRecord>()
                .FirstOrDefault(r => r.PlayerUuid == playerUuid);

            if (record == null)
                return DatabaseResult<PlayerState>.Fail(DatabaseErrorKind.None,
                    $"No save found for player '{playerUuid}'.");

            var state = PlayerState.FromJson(record.StateJson);
            return DatabaseResult<PlayerState>.Ok(state);
        }
        catch (Exception ex)
        {
            return DatabaseResult<PlayerState>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to load player state: {ex.Message}");
        }
    }

    public DatabaseResult DeletePlayerState(string playerUuid)
    {
        try
        {
            using var db = CreateConnection();
            db.Table<PlayerStateRecord>()
                .Delete(r => r.PlayerUuid == playerUuid);
            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to delete player state: {ex.Message}");
        }
    }

    public bool PlayerStateExists(string playerUuid)
    {
        try
        {
            using var db = CreateConnection();
            return db.Table<PlayerStateRecord>()
                .Any(r => r.PlayerUuid == playerUuid);
        }
        catch
        {
            return false;
        }
    }

    // ─── Map State ──────────────────────────────────────────

    public DatabaseResult<string> CreateMapState(string mapId, string stateJson = "{}")
    {
        try
        {
            using var db = CreateConnection();
            var id = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");

            db.Insert(new MapStateRecord
            {
                Id = id,
                MapId = mapId,
                StateJson = stateJson,
                CreatedAt = now,
                UpdatedAt = now,
            });

            return DatabaseResult<string>.Ok(id);
        }
        catch (Exception ex)
        {
            return DatabaseResult<string>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to create map state: {ex.Message}");
        }
    }

    public DatabaseResult<MapStateRecord> FindMapStateByMapId(string mapId)
    {
        try
        {
            using var db = CreateConnection();
            var record = db.Table<MapStateRecord>()
                .Where(r => r.MapId == mapId)
                .OrderByDescending(r => r.UpdatedAt)
                .FirstOrDefault();

            if (record == null)
                return DatabaseResult<MapStateRecord>.Fail(DatabaseErrorKind.None,
                    $"No map state found for '{mapId}'");

            // Check TTL expiry
            if (record.TtlUtc > 0)
            {
                long nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (nowUtc > record.TtlUtc)
                {
                    db.Execute("DELETE FROM map_object WHERE map_state_id = ?", record.Id);
                    db.Execute("DELETE FROM map_state WHERE id = ?", record.Id);
                    return DatabaseResult<MapStateRecord>.Fail(DatabaseErrorKind.None,
                        $"Map state for '{mapId}' expired (TTL)");
                }
            }

            return DatabaseResult<MapStateRecord>.Ok(record);
        }
        catch (Exception ex)
        {
            return DatabaseResult<MapStateRecord>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to find map state: {ex.Message}");
        }
    }

    public DatabaseResult SetMapStateTtl(string mapStateId, long ttlUtc)
    {
        try
        {
            using var db = CreateConnection();
            db.Execute("UPDATE map_state SET TtlUtc = ? WHERE id = ?", ttlUtc, mapStateId);
            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to set TTL: {ex.Message}");
        }
    }

    // ─── Map Objects ────────────────────────────────────────

    public DatabaseResult SaveMapObjects(string mapStateId, List<MapObjectRecord> objects)
    {
        try
        {
            using var db = CreateConnection();
            var now = DateTime.UtcNow.ToString("o");

            db.Execute("DELETE FROM map_object WHERE map_state_id = ?", mapStateId);

            foreach (var obj in objects)
            {
                obj.MapStateId = mapStateId;
                if (string.IsNullOrEmpty(obj.Id))
                    obj.Id = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(obj.CreatedAt))
                    obj.CreatedAt = now;
                obj.UpdatedAt = now;
                db.Insert(obj);
            }

            db.Execute("UPDATE map_state SET updated_at = ? WHERE id = ?", now, mapStateId);

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save objects: {ex.Message}");
        }
    }

    public DatabaseResult<List<MapObjectRecord>> LoadMapObjects(string mapStateId)
    {
        try
        {
            using var db = CreateConnection();
            var records = db.Table<MapObjectRecord>()
                .Where(r => r.MapStateId == mapStateId)
                .ToList();

            return DatabaseResult<List<MapObjectRecord>>.Ok(records);
        }
        catch (Exception ex)
        {
            return DatabaseResult<List<MapObjectRecord>>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to load objects: {ex.Message}");
        }
    }

    // ─── Object Effects ──────────────────────��──────────────

    public DatabaseResult SaveObjectEffects(string objectId, List<ObjectEffectRecord> effects)
    {
        try
        {
            using var db = CreateConnection();
            var now = DateTime.UtcNow.ToString("o");

            db.Execute("DELETE FROM object_effect WHERE object_id = ?", objectId);

            foreach (var eff in effects)
            {
                eff.ObjectId = objectId;
                if (string.IsNullOrEmpty(eff.Id))
                    eff.Id = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(eff.CreatedAt))
                    eff.CreatedAt = now;
                eff.UpdatedAt = now;
                db.Insert(eff);
            }

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save effects: {ex.Message}");
        }
    }

    public DatabaseResult<List<ObjectEffectRecord>> LoadObjectEffects(string objectId)
    {
        try
        {
            using var db = CreateConnection();
            var records = db.Table<ObjectEffectRecord>()
                .Where(r => r.ObjectId == objectId)
                .ToList();

            return DatabaseResult<List<ObjectEffectRecord>>.Ok(records);
        }
        catch (Exception ex)
        {
            return DatabaseResult<List<ObjectEffectRecord>>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to load effects: {ex.Message}");
        }
    }

    // ─── Backup ─────────────────────────────────────────────

    public DatabaseResult Backup()
    {
        if (!File.Exists(_databasePath))
            return DatabaseResult.Ok(); // Nothing to back up yet

        try
        {
            var backupDir = Path.Combine(_databaseDir, BackupDirectory);
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupDir, $"game_{timestamp}.db");
            File.Copy(_databasePath, backupPath, overwrite: true);

            // Clean old backups
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
            using var db = CreateConnection();
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

    /// <summary>
    /// Resolve the platform-specific database directory for log path access.
    /// </summary>
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

    // ──��� Filesystem Checks ──────────────────────────────────

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
