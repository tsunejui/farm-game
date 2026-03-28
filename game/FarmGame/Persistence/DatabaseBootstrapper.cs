using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace FarmGame.Persistence;

public class DatabaseBootstrapper
{
    private readonly string _databasePath;

    public DatabaseBootstrapper(string databasePath)
    {
        _databasePath = databasePath;
    }

    public DatabaseResult Initialize()
    {
        var spaceResult = CheckDiskSpace(Path.GetDirectoryName(_databasePath));
        if (!spaceResult.Success)
            return spaceResult;

        try
        {
            using var connection = CreateConnection();
            connection.Open();
            return MigrationManager.Migrate(connection);
        }
        catch (SqliteException ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to initialize database: {ex.Message}");
        }
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={_databasePath}");
    }

    private static DatabaseResult CheckDiskSpace(string directory)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(directory)));
            const long minFreeSpace = 10 * 1024 * 1024; // 10 MB
            if (driveInfo.AvailableFreeSpace < minFreeSpace)
            {
                return DatabaseResult.Fail(DatabaseErrorKind.DiskSpaceInsufficient,
                    "Insufficient disk space. At least 10 MB of free space is required.");
            }
        }
        catch
        {
            // DriveInfo may not work on all platforms/filesystems — proceed anyway
        }

        return DatabaseResult.Ok();
    }
}
