// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

internal sealed partial class SettingsPage : ListPage
{
    public SettingsPage()
    {
        Icon = new IconInfo(new IconData("\uE713")); // Settings icon
        Title = "Settings";
        Name = "Settings";
    }

    public override IListItem[] GetItems()
    {
        var settings = SettingsService.GetSettings();
        var currentEditor = settings.DefaultEditor ?? "notepad.exe";

        return
        [
            new ListItem(new EditSettingsCommand()) 
            { 
                Title = "Edit Settings", 
                Subtitle = "Open settings.json in configured editor",
                Icon = new IconInfo(new IconData("\uE70F")), // Edit icon
            },
            new ListItem(new OpenTemplatesDirectoryCommand()) 
            { 
                Title = "Open Templates Folder", 
                Subtitle = "Add .md template files to _templates\\",
                Icon = new IconInfo(new IconData("\uE8B7")), // Folder icon
            },
            new ListItem(new EditorConfigurationPage())
            {
                Title = "Configure Editor",
                Subtitle = $"Current: {currentEditor}",
                Icon = new IconInfo(new IconData("\uE70A")), // Edit icon
            },
            new ListItem(new ValidationResultsPage()) 
            { 
                Title = "Validate Settings", 
                Subtitle = "Check configuration for errors",
                Icon = new IconInfo(new IconData("\uE9D5")), // Check icon
            },
        ];
    }
}

internal sealed partial class ValidationResultsPage : ListPage
{
    public ValidationResultsPage()
    {
        Icon = new IconInfo(new IconData("\uE9D5")); // Check icon
        Title = "Validate Settings";
        Name = "Validate Settings";
    }

    public override IListItem[] GetItems()
    {
        var issues = SettingsValidator.GetIssues();
        if (issues.Count == 0)
        {
            return
            [
                new ListItem(new NoOpCommand())
                {
                    Title = "All settings are valid",
                    Subtitle = "Nothing to fix.",
                    Icon = new IconInfo(new IconData("\uE73E")), // Check mark
                },
            ];
        }

        var items = new System.Collections.Generic.List<IListItem>
        {
            new ListItem(new NoOpCommand())
            {
                Title = $"{issues.Count} issue(s) found",
                Subtitle = "Review the items below",
                Icon = new IconInfo(new IconData("\uE711")), // Warning
            },
        };

        foreach (var issue in issues)
        {
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = issue.Title,
                Subtitle = issue.Detail,
                Icon = new IconInfo(new IconData("\uE7BA")), // Error/bullet
            });
        }

        return items.ToArray();
    }
}

internal static class SettingsValidator
{
    public sealed record Issue(string Title, string Detail);

    public static System.Collections.Generic.List<Issue> GetIssues()
    {
        var issues = new System.Collections.Generic.List<Issue>();
        var settings = SettingsService.GetSettings();

        // Notes directory
        if (string.IsNullOrWhiteSpace(settings.NotesDirectory))
        {
            issues.Add(new Issue("Notes directory not set", @"Will fall back to Documents\QuickNotes."));
        }
        else if (!PathHelper.IsValidPath(settings.NotesDirectory))
        {
            issues.Add(new Issue("Invalid notes directory path", settings.NotesDirectory));
        }
        else
        {
            try
            {
                var fullPath = Path.GetFullPath(settings.NotesDirectory);
                if (!Directory.Exists(fullPath))
                {
                    issues.Add(new Issue("Notes directory does not exist", fullPath));
                }
            }
            catch (Exception ex)
            {
                issues.Add(new Issue("Could not validate notes directory", ex.Message));
            }
        }

        // Editor
        if (string.IsNullOrWhiteSpace(settings.DefaultEditor))
        {
            issues.Add(new Issue("Default editor not set", "Will fall back to notepad.exe."));
        }
        else if (settings.DefaultEditor.Contains(Path.DirectorySeparatorChar))
        {
            if (!File.Exists(settings.DefaultEditor))
            {
                issues.Add(new Issue("Configured editor not found", settings.DefaultEditor));
            }
        }

        return issues;
    }
}

public sealed partial class OpenDirectoryCommand : InvokableCommand
{
    private readonly string _directoryPath;

    public OpenDirectoryCommand(string directoryPath)
    {
        _directoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
        Icon = new IconInfo(new IconData("\uE8B7")); // Folder icon
    }

    public override ICommandResult Invoke()
    {
        try
        {
            // Validate path before use
            if (!PathHelper.IsValidPath(_directoryPath))
            {
                ToastNotificationHelper.ShowError("Invalid directory path.");
                return CommandResult.Dismiss();
            }

            // Ensure directory exists before trying to open
            var pathToOpen = _directoryPath;
            
            if (!Directory.Exists(pathToOpen))
            {
                try
                {
                    Directory.CreateDirectory(pathToOpen);
                }
                catch (Exception ex)
                {
                    ToastNotificationHelper.ShowWarning($"Could not create directory: {ex.Message}");
                    // Try to use parent directory
                    var parent = Path.GetDirectoryName(_directoryPath);
                    if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                    {
                        pathToOpen = parent;
                    }
                    else
                    {
                        pathToOpen = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                }
            }

            // Open in File Explorer
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{pathToOpen}\"",
                UseShellExecute = true, // Must be true for explorer
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to open directory: {ex.Message}");
            
            // Fallback: try opening documents folder as last resort
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    UseShellExecute = true,
                });
            }
            catch (Exception fallbackEx)
            {
                ToastNotificationHelper.ShowError($"Fallback also failed: {fallbackEx.Message}");
            }
        }

        return CommandResult.Dismiss();
    }
}

internal static class TemplatesHelper
{
    private static readonly string[] BuiltInTemplates = new[]
    {
        "Daily Journal.md", @"
# {{title}}

**Date:** {{date}}

## What I accomplished today

- 

## Notes

- 
",
        "Meeting Notes.md", @"
# {{title}}

**Date:** {{date}}
**Attendees:**

- 

## Agenda

1. 

## Discussion notes

- 

## Action items

- [ ] 
",
    };

    /// <summary>
    /// Returns the path to the _templates directory, creating it and seeding default
    /// templates if it doesn't exist.
    /// </summary>
    public static string EnsureTemplatesFolder()
    {
        var notesDir = SettingsService.GetSettings().NotesDirectory ?? PathHelper.GetDefaultNotesDirectory();
        var tplDir = System.IO.Path.Combine(notesDir, "_templates");
        if (!Directory.Exists(tplDir))
        {
            Directory.CreateDirectory(tplDir);
        }
        // Seed built-in templates if folder is empty
        if (Directory.GetFiles(tplDir, "*.md").Length == 0)
        {
            foreach (var tpl in BuiltInTemplates)
            {
                try { System.IO.File.WriteAllText(System.IO.Path.Combine(tplDir, tpl), tpl); } catch { }
            }
        }
        return tplDir;
    }
}

public sealed partial class OpenTemplatesDirectoryCommand : InvokableCommand
{
    public OpenTemplatesDirectoryCommand()
    {
        Icon = new IconInfo(new IconData("\uE8B7")); // Folder icon
    }

    public override ICommandResult Invoke()
    {
        try
        {
            var tplDir = TemplatesHelper.EnsureTemplatesFolder();
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{tplDir}\"",
                UseShellExecute = true,
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to open templates folder: {ex.Message}");
        }
        return CommandResult.Dismiss();
    }
}

public sealed partial class EditSettingsCommand : InvokableCommand
{
    public EditSettingsCommand()
    {
        Icon = new IconInfo(new IconData("\uE70F")); // Edit icon
    }

    public override ICommandResult Invoke()
    {
        var settingsPath = SettingsService.GetSettingsPath();

        // Validate settings path
        if (!PathHelper.IsValidPath(settingsPath))
        {
            ToastNotificationHelper.ShowError("Invalid settings file path.");
            return CommandResult.Dismiss();
        }

        // Ensure the file and directory exist
        SettingsService.EnsureSettingsFileExists();

        // Double-check file exists before trying to open
        if (!File.Exists(settingsPath))
        {
            // Try to create the file manually if EnsureSettingsFileExists failed
            try
            {
                var directory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var defaultSettings = new QuickNotesSettings
                {
                    NotesDirectory = PathHelper.GetDefaultNotesDirectory(),
                    DefaultEditor = "notepad.exe"
                };
                
                SettingsService.SaveSettings(defaultSettings);
            }
            catch (Exception ex)
            {
                ToastNotificationHelper.ShowError($"Failed to create settings file: {ex.Message}");
                return CommandResult.Dismiss();
            }
        }

        // Verify file exists before opening
        if (!File.Exists(settingsPath))
        {
            ToastNotificationHelper.ShowError("Settings file could not be created.");
            OpenSettingsDirectory(settingsPath);
            return CommandResult.Dismiss();
        }

        try
        {
            // Use full path and proper argument format for editor
            var fullPath = Path.GetFullPath(settingsPath);
            
            var settings = SettingsService.GetSettings();
            var editor = settings.DefaultEditor ?? "notepad.exe";

            // Validate editor if full path
            if (editor.Contains(Path.DirectorySeparatorChar) && !File.Exists(editor))
            {
                ToastNotificationHelper.ShowWarning($"Editor '{editor}' not found. Using notepad.exe.");
                editor = "notepad.exe";
            }
            
            var psi = new ProcessStartInfo
            {
                FileName = editor,
                Arguments = $"\"{fullPath}\"",
                UseShellExecute = true, // CRITICAL FIX: Must be true for external editors
                WorkingDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty,
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to open settings: {ex.Message}");
            // Fallback: try to open the directory
            OpenSettingsDirectory(settingsPath);
        }

        return CommandResult.Dismiss();
    }

    private static void OpenSettingsDirectory(string settingsPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = directory,
                    UseShellExecute = true,
                });
            }
            else
            {
                // Last resort: open LocalAppData
                Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to open directory: {ex.Message}");
        }
    }
}


