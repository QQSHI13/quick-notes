// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

public sealed partial class CreateNewNoteCommand : InvokableCommand
{
    public CreateNewNoteCommand()
    {
        Icon = new IconInfo(new IconData("\uE710")); // Add icon
    }

    public override ICommandResult Invoke()
    {
        var settings = SettingsService.GetSettings();
        var notesDir = settings.NotesDirectory ?? GetDefaultNotesDirectory();

        // Ensure directory exists
        if (!Directory.Exists(notesDir))
        {
            Directory.CreateDirectory(notesDir);
        }

        // Create timestamped filename
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        var fileName = $"Note_{timestamp}.md";
        var filePath = Path.Combine(notesDir, fileName);

        // Create file with default template
        var template = $"# Note {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}\n\n";
        File.WriteAllText(filePath, template);

        // Open in default editor
        OpenFile(filePath);

        return CommandResult.Dismiss();
    }

    private static string GetDefaultNotesDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "QuickNotes");
    }

    private static void OpenFile(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false,
            };
            Process.Start(psi);
        }
        catch
        {
            // Silently fail if we can't open the file
        }
    }
}

public sealed partial class OpenNoteCommand : InvokableCommand
{
    private readonly string _filePath;

    public OpenNoteCommand(string filePath)
    {
        _filePath = filePath;
        Icon = new IconInfo(new IconData("\uE8A5")); // Document icon
    }

    public override ICommandResult Invoke()
    {
        if (File.Exists(_filePath))
        {
            OpenFile(_filePath);
        }
        return CommandResult.Dismiss();
    }

    private static void OpenFile(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false,
            };
            Process.Start(psi);
        }
        catch
        {
            // Silently fail if we can't open the file
        }
    }
}

public sealed partial class ResetDirectoryCommand : InvokableCommand
{
    public ResetDirectoryCommand()
    {
        Icon = new IconInfo(new IconData("\uE72C")); // Refresh icon
    }

    public override ICommandResult Invoke()
    {
        var defaultDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "QuickNotes");

        var settings = SettingsService.GetSettings();
        settings.NotesDirectory = defaultDir;
        SettingsService.SaveSettings(settings);

        return CommandResult.GoBack();
    }
}

public sealed partial class SyncAllNoteTitlesCommand : InvokableCommand
{
    public SyncAllNoteTitlesCommand()
    {
        Icon = new IconInfo(new IconData("\uE8AC")); // Sync icon
    }

    public override ICommandResult Invoke()
    {
        var settings = SettingsService.GetSettings();
        var notesDirectory = settings.NotesDirectory ?? GetDefaultNotesDirectory();

        if (!Directory.Exists(notesDirectory))
        {
            return CommandResult.GoBack();
        }

        int syncedCount = 0;
        int skippedCount = 0;

        try
        {
            var mdFiles = Directory.GetFiles(notesDirectory, "*.md", SearchOption.TopDirectoryOnly);

            foreach (var filePath in mdFiles)
            {
                try
                {
                    var newFileName = GetSyncedFileName(filePath);
                    if (!string.IsNullOrEmpty(newFileName) && newFileName != Path.GetFileName(filePath))
                    {
                        var newFilePath = Path.Combine(notesDirectory, newFileName);
                        
                        // Ensure we don't overwrite an existing file
                        if (!File.Exists(newFilePath))
                        {
                            File.Move(filePath, newFilePath);
                            syncedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }
                catch
                {
                    // Skip files that can't be renamed
                    skippedCount++;
                }
            }
        }
        catch
        {
            // Silently fail
        }

        // Return to the list (which will refresh)
        return CommandResult.GoBack();
    }

    private static string GetDefaultNotesDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "QuickNotes");
    }

    private static string? GetSyncedFileName(string filePath)
    {
        try
        {
            // Read first few lines to find the title
            var lines = File.ReadLines(filePath).Take(10);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Look for markdown heading
                if (trimmed.StartsWith("# ", StringComparison.Ordinal) || trimmed.StartsWith('#'))
                {
                    var title = trimmed.TrimStart('#').Trim();
                    if (!string.IsNullOrEmpty(title) && !IsDefaultTitle(title))
                    {
                        // Sanitize filename
                        var safeName = SanitizeFileName(title);
                        if (!string.IsNullOrEmpty(safeName))
                        {
                            return safeName + ".md";
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        
        return null;
    }

    private static bool IsDefaultTitle(string title)
    {
        // Check if title matches default pattern like "Note 2025-03-10 12:34:56"
        return title.StartsWith("Note ", StringComparison.OrdinalIgnoreCase) && 
               Regex.IsMatch(title, @"Note\s+\d{4}-\d{2}-\d{2}");
    }

    private static string SanitizeFileName(string name)
    {
        // Remove invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        
        // Limit length
        if (safeName.Length > 50)
        {
            safeName = safeName.Substring(0, 50);
        }
        
        // Trim whitespace and dots
        safeName = safeName.Trim().TrimEnd('.');
        
        return safeName;
    }
}

public sealed partial class SyncNoteTitleCommand : InvokableCommand
{
    private readonly string _filePath;

    public SyncNoteTitleCommand(string filePath)
    {
        _filePath = filePath;
        Icon = new IconInfo(new IconData("\uE8AC")); // Sync icon
    }

    public override ICommandResult Invoke()
    {
        if (!File.Exists(_filePath))
        {
            return CommandResult.GoBack();
        }

        try
        {
            var newFileName = GetSyncedFileName(_filePath);
            if (!string.IsNullOrEmpty(newFileName) && newFileName != Path.GetFileName(_filePath))
            {
                var directory = Path.GetDirectoryName(_filePath);
                var newFilePath = Path.Combine(directory!, newFileName);
                
                // Ensure we don't overwrite an existing file
                if (!File.Exists(newFilePath))
                {
                    File.Move(_filePath, newFilePath);
                }
            }
        }
        catch
        {
            // Silently fail if rename doesn't work
        }

        return CommandResult.GoBack();
    }

    private static string? GetSyncedFileName(string filePath)
    {
        try
        {
            // Read first few lines to find the title
            var lines = File.ReadLines(filePath).Take(10);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Look for markdown heading
                if (trimmed.StartsWith("# ", StringComparison.Ordinal) || trimmed.StartsWith('#'))
                {
                    var title = trimmed.TrimStart('#').Trim();
                    if (!string.IsNullOrEmpty(title) && !IsDefaultTitle(title))
                    {
                        // Sanitize filename
                        var safeName = SanitizeFileName(title);
                        if (!string.IsNullOrEmpty(safeName))
                        {
                            return safeName + ".md";
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        
        return null;
    }

    private static bool IsDefaultTitle(string title)
    {
        return title.StartsWith("Note ", StringComparison.OrdinalIgnoreCase) && 
               Regex.IsMatch(title, @"Note\s+\d{4}-\d{2}-\d{2}");
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        
        if (safeName.Length > 50)
        {
            safeName = safeName.Substring(0, 50);
        }
        
        safeName = safeName.Trim().TrimEnd('.');
        
        return safeName;
    }
}
