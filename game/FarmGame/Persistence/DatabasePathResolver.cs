using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FarmGame.Persistence;

public static class DatabasePathResolver
{
    private const string DatabaseFileName = "game.db";

    public static string GetDatabaseDirectory(string gameName)
    {
        string safeName = SanitizeName(gameName);
        string baseDir;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // %LOCALAPPDATA%/FarmGame/
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // ~/Library/Application Support/FarmGame/
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else
        {
            // Linux: $XDG_DATA_HOME/FarmGame/ or ~/.local/share/FarmGame/
            baseDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrEmpty(baseDir))
            {
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "share");
            }
        }

        return Path.Combine(baseDir, safeName);
    }

    public static string GetDatabasePath(string gameName)
    {
        return Path.Combine(GetDatabaseDirectory(gameName), DatabaseFileName);
    }

    public static DatabaseResult EnsureDirectoryExists(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return DatabaseResult.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.PermissionDenied,
                $"Permission denied: cannot create directory '{path}'.");
        }
        catch (IOException ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.DirectoryCreationFailed,
                $"Failed to create directory '{path}': {ex.Message}");
        }
    }

    public static DatabaseResult CheckWritePermission(string path)
    {
        string testFile = Path.Combine(path, ".write_test");
        try
        {
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return DatabaseResult.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.PermissionDenied,
                $"Permission denied: cannot write to '{path}'.");
        }
        catch (IOException ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.PermissionDenied,
                $"Cannot write to '{path}': {ex.Message}");
        }
    }

    private static string SanitizeName(string name)
    {
        // Replace spaces with underscores, strip non-alphanumeric except underscores
        string sanitized = name.Replace(' ', '_');
        sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9_]", "");
        return string.IsNullOrEmpty(sanitized) ? "FarmGame" : sanitized;
    }
}
