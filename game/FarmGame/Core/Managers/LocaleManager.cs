using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Serilog;

namespace FarmGame.Core.Managers;

public static class LocaleManager
{
    private static readonly Dictionary<string, Dictionary<string, string>> _current = new();
    private static readonly Dictionary<string, Dictionary<string, string>> _fallback = new();

    public static string CurrentLanguage { get; private set; } = "en";

    /// <summary>
    /// Load locale files from the given locales directory.
    /// </summary>
    /// <param name="localesDir">Path to the locales root (contains en/, zh-TW/, etc.)</param>
    /// <param name="language">Language code to load.</param>
    public static void Load(string localesDir, string language)
    {
        CurrentLanguage = language;
        _current.Clear();

        LoadLanguage(localesDir, language, _current);

        if (language != "en")
        {
            _fallback.Clear();
            LoadLanguage(localesDir, "en", _fallback);
        }

        Log.Information("Locale loaded: {Language}", language);
    }

    public static string Get(string module, string key, string fallback = null)
    {
        if (_current.TryGetValue(module, out var currentModule) &&
            currentModule.TryGetValue(key, out var value))
            return value;

        if (_fallback.TryGetValue(module, out var fallbackModule) &&
            fallbackModule.TryGetValue(key, out var fbValue))
            return fbValue;

        return fallback ?? key;
    }

    public static string Format(string module, string key, params object[] args)
    {
        var template = Get(module, key);
        return string.Format(template, args);
    }

    private static void LoadLanguage(string localesDir, string language,
        Dictionary<string, Dictionary<string, string>> target)
    {
        var langDir = Path.Combine(localesDir, language);
        if (!Directory.Exists(langDir))
        {
            Log.Warning("Locale directory not found: {Dir}", langDir);
            return;
        }

        foreach (var file in Directory.GetFiles(langDir, "*.json"))
        {
            var module = Path.GetFileNameWithoutExtension(file);
            try
            {
                var json = File.ReadAllText(file);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                    target[module] = dict;
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load locale file {File}: {Error}", file, ex.Message);
            }
        }
    }
}
