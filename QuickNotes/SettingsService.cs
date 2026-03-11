// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickNotes;

public sealed class QuickNotesSettings
{
    public string? NotesDirectory { get; set; }
}

[JsonSerializable(typeof(QuickNotesSettings))]
internal sealed partial class QuickNotesJsonContext : JsonSerializerContext
{
}

public static class SettingsService
{
    public static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuickNotes",
        "settings.json");

    private static readonly QuickNotesJsonContext JsonContext = new(new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    });

    private static QuickNotesSettings? _cachedSettings;

    public static string GetSettingsPath() => SettingsPath;

    public static void EnsureSettingsFileExists()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(SettingsPath))
            {
                var defaultSettings = new QuickNotesSettings
                {
                    NotesDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "QuickNotes")
                };
                SaveSettings(defaultSettings);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    public static QuickNotesSettings GetSettings()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _cachedSettings = JsonSerializer.Deserialize(json, JsonContext.QuickNotesSettings);
            }
        }
        catch
        {
            // Ignore errors, use default
        }

        _cachedSettings ??= new QuickNotesSettings();
        return _cachedSettings;
    }

    public static void SaveSettings(QuickNotesSettings settings)
    {
        _cachedSettings = settings;
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonContext.QuickNotesSettings);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore errors
        }
    }
}
