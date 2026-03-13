// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        var currentDir = settings.NotesDirectory ?? PathHelper.GetDefaultNotesDirectory();
        var currentEditor = settings.DefaultEditor ?? "notepad.exe";

        return
        [
            new ListItem(new OpenDirectoryCommand(currentDir)) 
            { 
                Title = "Current Directory", 
                Subtitle = currentDir,
                Icon = new IconInfo(new IconData("\uE8B7")), // Folder icon
            },
            new ListItem(new EditSettingsCommand()) 
            { 
                Title = "Edit Settings", 
                Subtitle = "Open settings.json in configured editor",
                Icon = new IconInfo(new IconData("\uE70F")), // Edit icon
            },
            new ListItem(new ConfigureEditorCommand()) 
            { 
                Title = "Configure Editor", 
                Subtitle = $"Current: {currentEditor}",
                Icon = new IconInfo(new IconData("\uE70A")), // Edit icon
            },
            new ListItem(new ResetDirectoryCommand()) 
            { 
                Title = "Reset to Default", 
                Subtitle = "Set to Documents\\QuickNotes",
                Icon = new IconInfo(new IconData("\uE72C")), // Refresh/Reset icon
            },
            new ListItem(new ValidateSettingsCommand()) 
            { 
                Title = "Validate Settings", 
                Subtitle = "Check configuration for errors",
                Icon = new IconInfo(new IconData("\uE9D5")), // Check icon
            },
        ];
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

public sealed partial class ConfigureEditorCommand : InvokableCommand
{
    public ConfigureEditorCommand()
    {
        Icon = new IconInfo(new IconData("\uE70A")); // Edit icon
    }

    public override ICommandResult Invoke()
    {
        // Show editor configuration (ShowForm not available in this SDK version)
        // return CommandResult.ShowForm(new EditorConfigurationPage());
        // Fallback: show message that editor configuration is not available
        ToastNotificationHelper.ShowInfo("Editor configuration requires updated SDK");
        return CommandResult.GoBack();
    }
}

public sealed partial class ValidateSettingsCommand : InvokableCommand
{
    public ValidateSettingsCommand()
    {
        Icon = new IconInfo(new IconData("\uE9D5")); // Check icon
    }

    public override ICommandResult Invoke()
    {
        var issues = new System.Collections.Generic.List<string>();
        var settings = SettingsService.GetSettings();

        // Validate notes directory
        if (string.IsNullOrWhiteSpace(settings.NotesDirectory))
        {
            issues.Add("Notes directory is not set (will use default)");
        }
        else if (!PathHelper.IsValidPath(settings.NotesDirectory))
        {
            issues.Add($"Invalid notes directory path: {settings.NotesDirectory}");
        }
        else
        {
            try
            {
                var fullPath = Path.GetFullPath(settings.NotesDirectory);
                if (!Directory.Exists(fullPath))
                {
                    issues.Add($"Notes directory does not exist: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Error validating notes directory: {ex.Message}");
            }
        }

        // Validate editor
        if (string.IsNullOrWhiteSpace(settings.DefaultEditor))
        {
            issues.Add("Default editor is not set (will use notepad.exe)");
        }
        else if (settings.DefaultEditor.Contains(Path.DirectorySeparatorChar))
        {
            if (!File.Exists(settings.DefaultEditor))
            {
                issues.Add($"Configured editor not found: {settings.DefaultEditor}");
            }
        }

        // Show results
        if (issues.Count == 0)
        {
            ToastNotificationHelper.ShowSuccess("All settings are valid!");
        }
        else
        {
            ToastNotificationHelper.ShowWarning($"Found {issues.Count} issue(s). Check debug output for details.");
            foreach (var issue in issues)
            {
                Debug.WriteLine($"[SETTINGS VALIDATION] {issue}");
            }
        }

        return CommandResult.GoBack();
    }
}
