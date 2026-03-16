// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    private readonly string? _template;

    public CreateNewNoteCommand(string? template = null)
    {
        Icon = new IconInfo(new IconData("\uE710")); // Add icon
        _template = template;
    }

    public override ICommandResult Invoke()
    {
        try
        {
            var settings = SettingsService.GetSettings();
            var notesDir = settings.NotesDirectory ?? PathHelper.GetDefaultNotesDirectory();

            // Validate notes directory
            if (!PathHelper.IsValidPath(notesDir))
            {
                ToastNotificationHelper.ShowError("Invalid notes directory path configured.");
                return CommandResult.Dismiss();
            }

            // Ensure directory exists
            if (!Directory.Exists(notesDir))
            {
                Directory.CreateDirectory(notesDir);
            }

            // Create timestamped filename
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            var fileName = $"Note_{timestamp}.md";
            var filePath = Path.Combine(notesDir, fileName);

            // Create file with template
            var template = _template ?? $"# Note {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}\n\n";
            File.WriteAllText(filePath, template);

            // Open in configured editor
            if (!OpenFileHelper.OpenFileWithEditor(filePath))
            {
                return CommandResult.Dismiss();
            }

            // Track as recent note
            RecentNotesService.AddRecentNote(filePath);

            return CommandResult.Dismiss();
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to create note: {ex.Message}");
            return CommandResult.Dismiss();
        }
    }
}

public sealed partial class OpenNoteCommand : InvokableCommand
{
    private readonly string _filePath;

    public OpenNoteCommand(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Icon = new IconInfo(new IconData("\uE8A5")); // Document icon
    }

    public override ICommandResult Invoke()
    {
        if (!File.Exists(_filePath))
        {
            ToastNotificationHelper.ShowError("Note file no longer exists.");
            return CommandResult.Dismiss();
        }

        // Track as recent note
        RecentNotesService.AddRecentNote(_filePath);

        if (!OpenFileHelper.OpenFileWithEditor(_filePath))
        {
            return CommandResult.Dismiss();
        }

        return CommandResult.Dismiss();
    }
}

public sealed partial class DeleteNoteCommand : InvokableCommand
{
    private readonly string _filePath;

    public DeleteNoteCommand(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Icon = new IconInfo(new IconData("\uE74D")); // Delete icon
    }

    public override ICommandResult Invoke()
    {
        if (!File.Exists(_filePath))
        {
            ToastNotificationHelper.ShowError("Note file no longer exists.");
            return CommandResult.GoBack();
        }

        try
        {
            var fileName = Path.GetFileName(_filePath);
            File.Delete(_filePath);
            RecentNotesService.RemoveRecentNote(_filePath);
            ToastNotificationHelper.ShowSuccess($"Deleted '{fileName}'");
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to delete note: {ex.Message}");
        }

        return CommandResult.GoBack();
    }
}

public sealed partial class ConfirmDeleteNoteCommand : InvokableCommand
{
    private readonly string _filePath;

    public ConfirmDeleteNoteCommand(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Icon = new IconInfo(new IconData("\uE74D")); // Delete icon
    }

    public override ICommandResult Invoke()
    {
        if (!File.Exists(_filePath))
        {
            ToastNotificationHelper.ShowError("Note file no longer exists.");
            return CommandResult.GoBack();
        }

        var fileName = Path.GetFileName(_filePath);
        
        // Show confirmation page (using fallback - ShowPage not available in this SDK version)
        // return CommandResult.ShowForm(new DeleteConfirmationPage(_filePath, fileName));
        // Fallback: just go back and let user manually delete
        return CommandResult.GoBack();
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
        try
        {
            var defaultDir = PathHelper.GetDefaultNotesDirectory();

            var settings = SettingsService.GetSettings();
            settings.NotesDirectory = defaultDir;
            SettingsService.SaveSettings(settings);

            ToastNotificationHelper.ShowSuccess("Directory reset to default");
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to reset directory: {ex.Message}");
        }

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
        var notesDirectory = settings.NotesDirectory ?? PathHelper.GetDefaultNotesDirectory();

        if (!Directory.Exists(notesDirectory))
        {
            ToastNotificationHelper.ShowError("Notes directory does not exist.");
            return CommandResult.GoBack();
        }

        int syncedCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

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
                        
                        // TOCTOU fix: Check and move atomically where possible
                        if (!File.Exists(newFilePath))
                        {
                            try
                            {
                                File.Move(filePath, newFilePath);
                                syncedCount++;
                                
                                // Update recent notes if path changed
                                RecentNotesService.UpdateNotePath(filePath, newFilePath);
                            }
                            catch (IOException)
                            {
                                // File may have been created between check and move
                                skippedCount++;
                            }
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Debug.WriteLine($"Error syncing file {filePath}: {ex.Message}");
                }
            }

            // Show feedback
            if (syncedCount > 0)
            {
                ToastNotificationHelper.ShowSuccess($"Synced {syncedCount} note(s)." + 
                    (skippedCount > 0 ? $" Skipped {skippedCount}." : "") +
                    (errorCount > 0 ? $" Errors: {errorCount}." : ""));
            }
            else if (skippedCount > 0 || errorCount > 0)
            {
                ToastNotificationHelper.ShowWarning($"No notes synced. Skipped: {skippedCount}, Errors: {errorCount}");
            }
            else
            {
                ToastNotificationHelper.ShowInfo("All notes already have matching titles.");
            }
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Sync failed: {ex.Message}");
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading file {filePath}: {ex.Message}");
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
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Icon = new IconInfo(new IconData("\uE8AC")); // Sync icon
    }

    public override ICommandResult Invoke()
    {
        if (!File.Exists(_filePath))
        {
            ToastNotificationHelper.ShowError("Note file no longer exists.");
            return CommandResult.GoBack();
        }

        try
        {
            var newFileName = GetSyncedFileName(_filePath);
            if (!string.IsNullOrEmpty(newFileName) && newFileName != Path.GetFileName(_filePath))
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (string.IsNullOrEmpty(directory))
                {
                    ToastNotificationHelper.ShowError("Invalid file path.");
                    return CommandResult.GoBack();
                }
                
                var newFilePath = Path.Combine(directory, newFileName);
                
                // TOCTOU fix: Check and move with error handling
                if (!File.Exists(newFilePath))
                {
                    try
                    {
                        File.Move(_filePath, newFilePath);
                        RecentNotesService.UpdateNotePath(_filePath, newFilePath);
                        ToastNotificationHelper.ShowSuccess($"Renamed to '{newFileName}'");
                    }
                    catch (IOException)
                    {
                        ToastNotificationHelper.ShowWarning("Could not rename: target file already exists.");
                    }
                }
                else
                {
                    ToastNotificationHelper.ShowWarning("A file with that name already exists.");
                }
            }
            else
            {
                ToastNotificationHelper.ShowInfo("Title already matches filename.");
            }
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to sync title: {ex.Message}");
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading file {filePath}: {ex.Message}");
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

public sealed partial class NoOpCommand : InvokableCommand
{
    public override ICommandResult Invoke() => CommandResult.Dismiss();
}

internal static class PathHelper
{
    public static string GetDefaultNotesDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "QuickNotes");
    }

    public static bool IsValidPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // Check for invalid characters
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            // Try to get full path - this validates the path format
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

internal static class OpenFileHelper
{
    public static bool OpenFileWithEditor(string filePath, string? editorPath = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            ToastNotificationHelper.ShowError("Invalid file path.");
            return false;
        }

        if (!File.Exists(filePath))
        {
            ToastNotificationHelper.ShowError("File does not exist.");
            return false;
        }

        try
        {
            var settings = SettingsService.GetSettings();
            var editor = editorPath ?? settings.DefaultEditor ?? "notepad.exe";

            // Validate editor path if it's a full path
            if (editor.Contains(Path.DirectorySeparatorChar) && !File.Exists(editor))
            {
                ToastNotificationHelper.ShowWarning($"Configured editor not found: {editor}. Falling back to notepad.");
                editor = "notepad.exe";
            }

            var psi = new ProcessStartInfo
            {
                FileName = editor,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true, // CRITICAL: Must be true for opening files with external apps
            };
            
            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to open file: {ex.Message}");
            return false;
        }
    }
}

internal static class ToastNotificationHelper
{
    public static void ShowSuccess(string message)
    {
        // In a real implementation, this would show a Windows toast notification
        // For now, we use Debug.WriteLine for logging
        Debug.WriteLine($"[SUCCESS] {message}");
    }

    public static void ShowError(string message)
    {
        Debug.WriteLine($"[ERROR] {message}");
    }

    public static void ShowWarning(string message)
    {
        Debug.WriteLine($"[WARNING] {message}");
    }

    public static void ShowInfo(string message)
    {
        Debug.WriteLine($"[INFO] {message}");
    }
}
