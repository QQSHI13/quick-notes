// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        var currentDir = settings.NotesDirectory ?? GetDefaultNotesDirectory();

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
                Subtitle = "Open settings.json in Notepad",
                Icon = new IconInfo(new IconData("\uE70F")), // Edit icon
            },
            new ListItem(new ResetDirectoryCommand()) 
            { 
                Title = "Reset to Default", 
                Subtitle = "Set to Documents\\QuickNotes",
                Icon = new IconInfo(new IconData("\uE72C")), // Refresh/Reset icon
            },
        ];
    }

    private static string GetDefaultNotesDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "QuickNotes");
    }
}

public sealed partial class OpenDirectoryCommand : InvokableCommand
{
    private readonly string _directoryPath;

    public OpenDirectoryCommand(string directoryPath)
    {
        _directoryPath = directoryPath;
        Icon = new IconInfo(new IconData("\uE8B7")); // Folder icon
    }

    public override ICommandResult Invoke()
    {
        try
        {
            // Ensure directory exists before trying to open
            if (!Directory.Exists(_directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(_directoryPath);
                }
                catch
                {
                    // If we can't create it, fallback to parent or default
                }
            }

            var pathToOpen = _directoryPath;
            
            // If directory doesn't exist, try to open parent or documents
            if (!Directory.Exists(pathToOpen))
            {
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

            // Open in File Explorer
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{pathToOpen}\"",
                UseShellExecute = true,
            };
            Process.Start(psi);
        }
        catch
        {
            // Silently fail - try opening documents folder as last resort
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    UseShellExecute = true,
                });
            }
            catch
            {
                // Give up
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
                    NotesDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "QuickNotes")
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(defaultSettings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                });
                
                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                return CommandResult.Dismiss();
            }
        }

        // Verify file exists before opening
        if (!File.Exists(settingsPath))
        {
            OpenSettingsDirectory(settingsPath);
            return CommandResult.Dismiss();
        }

        try
        {
            // Use full path and proper argument format for notepad
            var fullPath = Path.GetFullPath(settingsPath);
            
            var psi = new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = fullPath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty,
            };
            Process.Start(psi);
        }
        catch
        {
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
        catch
        {
            // Silently fail
        }
    }
}
