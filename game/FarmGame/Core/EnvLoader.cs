using System;
using System.IO;

namespace FarmGame.Core;

/// <summary>
/// Loads key-value pairs from .env.local into environment variables.
/// Searches upward from the executable directory to find the file.
/// </summary>
public static class EnvLoader
{
    public static void Load()
    {
        var envPath = FindEnvFile();
        if (envPath == null) return;

        foreach (var line in File.ReadLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex <= 0) continue;

            var key = trimmed[..eqIndex].Trim();
            var value = trimmed[(eqIndex + 1)..].Trim().Trim('"');

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string FindEnvFile()
    {
        // Start from executable directory, strip trailing separator
        var dir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Search upward up to 10 levels
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, ".env.local");
            if (File.Exists(candidate))
                return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        return null;
    }
}
