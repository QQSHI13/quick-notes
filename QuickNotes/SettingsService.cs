// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickNotes;

public sealed class QuickNotesSettings
{
    public string? NotesDirectory { get; set; }
    public string? DefaultEditor { get; set; }
    public List<string> RecentNotes { get; set; } = new();
    public int MaxRecentNotes { get; set; } = 10;
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
    private static readonly object _settingsLock = new();

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
                CreateSettingsWithComments();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SETTINGS] Error ensuring settings file exists: {ex.Message}");
        }
    }

    private static void CreateSettingsWithComments()
    {
        var defaultDir = PathHelper.GetDefaultNotesDirectory();
        var settingsContent = """
{
  "_comment1": "Quick Notes Extension Settings",
  "_comment2": "notesDirectory: Where your markdown notes are saved",
  "_comment3": "defaultEditor: App to open notes (notepad.exe, code.exe, etc.)",
  "_comment4": "maxRecentNotes: Number of recent notes to remember (1-50)",
  "notesDirectory": "{{DEFAULT_DIR}}",
  "defaultEditor": "notepad.exe",
  "maxRecentNotes": 10,
  "recentNotes": []
}
""".Replace("{{DEFAULT_DIR}}", defaultDir.Replace("\\", "\\\\"));

        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(SettingsPath, settingsContent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SETTINGS] Error creating settings with comments: {ex.Message}");
            // Fallback to JSON serialization
            var defaultSettings = new QuickNotesSettings
            {
                NotesDirectory = PathHelper.GetDefaultNotesDirectory(),
                DefaultEditor = "notepad.exe",
                RecentNotes = new List<string>(),
                MaxRecentNotes = 10
            };
            SaveSettings(defaultSettings);
        }
    }

    public static QuickNotesSettings GetSettings()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        lock (_settingsLock)
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize(json, JsonContext.QuickNotesSettings);
                    if (settings != null)
                    {
                        // Validate and sanitize
                        if (settings.MaxRecentNotes <= 0 || settings.MaxRecentNotes > 50)
                        {
                            settings.MaxRecentNotes = 10;
                        }
                        
                        // Clean up recent notes list - remove non-existent files
                        if (settings.RecentNotes != null)
                        {
                            settings.RecentNotes.RemoveAll(path => !File.Exists(path));
                            
                            // Trim to max
                            while (settings.RecentNotes.Count > settings.MaxRecentNotes)
                            {
                                settings.RecentNotes.RemoveAt(settings.RecentNotes.Count - 1);
                            }
                        }
                        else
                        {
                            settings.RecentNotes = new List<string>();
                        }

                        _cachedSettings = settings;
                        return _cachedSettings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] Error reading settings: {ex.Message}");
            }

            _cachedSettings = new QuickNotesSettings
            {
                NotesDirectory = PathHelper.GetDefaultNotesDirectory(),
                DefaultEditor = "notepad.exe",
                RecentNotes = new List<string>(),
                MaxRecentNotes = 10
            };
            return _cachedSettings;
        }
    }

    public static void SaveSettings(QuickNotesSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_settingsLock)
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] Error saving settings: {ex.Message}");
            }
        }
    }

    public static void ClearCache()
    {
        lock (_settingsLock)
        {
            _cachedSettings = null;
        }
    }
}

public static class RecentNotesService
{
    public static void AddRecentNote(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            var settings = SettingsService.GetSettings();
            
            // Remove if already exists (to move to top)
            settings.RecentNotes.Remove(filePath);
            
            // Add to beginning
            settings.RecentNotes.Insert(0, filePath);
            
            // Trim to max
            while (settings.RecentNotes.Count > settings.MaxRecentNotes)
            {
                settings.RecentNotes.RemoveAt(settings.RecentNotes.Count - 1);
            }
            
            SettingsService.SaveSettings(settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RECENT NOTES] Error adding recent note: {ex.Message}");
        }
    }

    public static void RemoveRecentNote(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            var settings = SettingsService.GetSettings();
            settings.RecentNotes.Remove(filePath);
            SettingsService.SaveSettings(settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RECENT NOTES] Error removing recent note: {ex.Message}");
        }
    }

    public static void UpdateNotePath(string oldPath, string newPath)
    {
        if (string.IsNullOrWhiteSpace(oldPath) || string.IsNullOrWhiteSpace(newPath))
            return;

        try
        {
            var settings = SettingsService.GetSettings();
            var index = settings.RecentNotes.IndexOf(oldPath);
            if (index >= 0)
            {
                settings.RecentNotes[index] = newPath;
                SettingsService.SaveSettings(settings);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RECENT NOTES] Error updating note path: {ex.Message}");
        }
    }

    public static List<string> GetRecentNotes()
    {
        try
        {
            var settings = SettingsService.GetSettings();
            
            // Filter out non-existent files
            var validNotes = settings.RecentNotes.Where(File.Exists).ToList();
            
            // Update if we removed any
            if (validNotes.Count != settings.RecentNotes.Count)
            {
                settings.RecentNotes = validNotes;
                SettingsService.SaveSettings(settings);
            }
            
            return validNotes;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RECENT NOTES] Error getting recent notes: {ex.Message}");
            return new List<string>();
        }
    }

    public static void ClearRecentNotes()
    {
        try
        {
            var settings = SettingsService.GetSettings();
            settings.RecentNotes.Clear();
            SettingsService.SaveSettings(settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RECENT NOTES] Error clearing recent notes: {ex.Message}");
        }
    }
}
