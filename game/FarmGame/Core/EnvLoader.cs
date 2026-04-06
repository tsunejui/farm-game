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
        var dir = AppDomain.CurrentDomain.BaseDirectory;

        // Search upward up to 6 levels to find .env.local
        for (int i = 0; i < 6; i++)
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
