using System;
using System.IO;

namespace FarmGame.Persistence;

public static class DatabaseBackup
{
    private const string BackupDirectory = "backups";
    private const int MaxBackups = 5;

    public static DatabaseResult Backup(string databasePath)
    {
        if (!File.Exists(databasePath))
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                "Database file not found.");

        try
        {
            var backupDir = Path.Combine(Path.GetDirectoryName(databasePath), BackupDirectory);
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"game_{timestamp}.db";
            var backupPath = Path.Combine(backupDir, backupFileName);

            File.Copy(databasePath, backupPath, overwrite: true);

            CleanOldBackups(backupDir);

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Backup failed: {ex.Message}");
        }
    }

    private static void CleanOldBackups(string backupDir)
    {
        var files = new DirectoryInfo(backupDir)
            .GetFiles("game_*.db");

        if (files.Length <= MaxBackups)
            return;

        Array.Sort(files, (a, b) => a.CreationTimeUtc.CompareTo(b.CreationTimeUtc));

        for (int i = 0; i < files.Length - MaxBackups; i++)
        {
            files[i].Delete();
        }
    }
}
