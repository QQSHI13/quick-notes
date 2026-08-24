// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

#nullable enable

using System;
using System.Collections.Generic;
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
    private readonly Action? _refreshParent;

    public CreateNewNoteCommand(string? template = null, Action? refreshParent = null)
    {
        Icon = new IconInfo(new IconData("\uE710")); // Add icon
        _template = template;
        _refreshParent = refreshParent;
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

            // Create timestamped filename (with collision avoidance so rapid
            // note creation never silently overwrites an existing note)
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            var filePath = PathHelper.GetUniqueFilePath(notesDir, $"Note_{timestamp}", ".md");

            // Create file with template (replace placeholders)
            var template = _template ?? $"# Note {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}\n\n";
            if (!string.IsNullOrEmpty(_template))
            {
                var now = DateTime.Now;
                template = _template
                    .Replace("{{date}}", now.ToString("yyyy-MM-dd"))
                    .Replace("{{time}}", now.ToString("HH:mm:ss"))
                    .Replace("{{datetime}}", now.ToString("yyyy-MM-dd HH:mm:ss"))
                    .Replace("{{title}}", "Note");
            }
            File.WriteAllText(filePath, template);

            // Open in configured editor
            if (!OpenFileHelper.OpenFileWithEditor(filePath))
            {
                return CommandResult.Dismiss();
            }

            // Track as recent note
            RecentNotesService.AddRecentNote(filePath);

            // Notify any parent list page (e.g. Open Existing) to refresh, since a
            // new note now exists on disk.
            _refreshParent?.Invoke();

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
    private readonly Action? _refreshParent;

    public DeleteNoteCommand(string filePath, Action? refreshParent = null)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _refreshParent = refreshParent;
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
            // Tell the parent list to refresh so the deleted note disappears.
            _refreshParent?.Invoke();
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to delete note: {ex.Message}");
        }

        return CommandResult.GoBack();
    }
}

public sealed partial class ResetDirectoryCommand : InvokableCommand
{
    private readonly Action? _refreshParent;

    public ResetDirectoryCommand(Action? refreshParent = null)
    {
        _refreshParent = refreshParent;
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
            // Tell the settings page to refresh so the new directory is shown.
            _refreshParent?.Invoke();
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
                    var newFileName = NoteTitleHelper.GetSyncedFileName(filePath);
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
}

public sealed partial class SyncNoteTitleCommand : InvokableCommand
{
    private readonly string _filePath;
    private readonly Action? _refreshParent;

    public SyncNoteTitleCommand(string filePath, Action? refreshParent = null)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _refreshParent = refreshParent;
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
            var newFileName = NoteTitleHelper.GetSyncedFileName(_filePath);
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
                        // Tell the parent list to refresh so the renamed note shows its new name.
                        _refreshParent?.Invoke();
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

    /// <summary>
    /// Returns a non-colliding file path in <paramref name="directory"/> based on the
    /// desired <paramref name="baseName"/> and <paramref name="extension"/>. If the
    /// target exists, appends " (2)", " (3)", ... until a free name is found.
    /// </summary>
    public static string GetUniqueFilePath(string directory, string baseName, string extension)
    {
        var candidate = Path.Combine(directory, baseName + extension);
        var counter = 2;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{baseName} ({counter}){extension}");
            counter++;
        }
        return candidate;
    }
}

internal static class NoteTitleHelper
{
    /// <summary>
    /// Extracts the first markdown heading title from a note file, or null if none found.
    /// Shared by the note list (display) and the rename/sync commands so both agree.
    /// </summary>
    public static string? ExtractTitle(string filePath)
    {
        try
        {
            var lines = File.ReadLines(filePath).Take(10);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Look for markdown heading: "# Title", "## Title", etc.
                if (trimmed.StartsWith("# ", StringComparison.Ordinal) || trimmed.StartsWith('#'))
                {
                    var title = trimmed.TrimStart('#').Trim();
                    if (!string.IsNullOrEmpty(title))
                    {
                        return title;
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

    public static string? GetSyncedFileName(string filePath)
    {
        var title = ExtractTitle(filePath);
        if (string.IsNullOrEmpty(title) || IsDefaultTitle(title))
        {
            return null;
        }

        var safeName = SanitizeFileName(title);
        return string.IsNullOrEmpty(safeName) ? null : safeName + ".md";
    }

    // Compiled regex for default title detection (Note YYYY-MM-DD HH:MM:SS)
    private static readonly Regex DefaultTitleRegex = new(
        @"^Note\s+\d{4}-\d{2}-\d{2}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsDefaultTitle(string title)
    {
        return DefaultTitleRegex.IsMatch(title);
    }

    // Names Windows reserves for DOS devices. A file cannot be created with one of
    // these as its base name, with or without an extension, so a note titled
    // "# NUL" would otherwise produce an unwritable path.
    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());

        if (safeName.Length > 50)
        {
            safeName = safeName.Substring(0, 50);
        }
        
        safeName = safeName.Trim().TrimEnd('.');

        // Suffix reserved device names so the resulting path is creatable. Callers
        // treat an empty result as "no usable name", so leave that case untouched.
        if (safeName.Length > 0 && ReservedDeviceNames.Contains(safeName))
        {
            safeName += "_";
        }

        return safeName;
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

public sealed partial class TogglePinNoteCommand : InvokableCommand
{
    private readonly string _filePath;
    private readonly bool _pin;
    private readonly Action? _refreshParent;

    public TogglePinNoteCommand(string filePath, bool pin, Action? refreshParent = null)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _pin = pin;
        _refreshParent = refreshParent;
        Icon = new IconInfo(new IconData(_pin ? "\uE735" : "\uE718")); // Pin / Unpin icon
    }

    public override ICommandResult Invoke()
    {
        var settings = SettingsService.GetSettings();
        if (_pin)
        {
            if (!settings.PinnedNotes.Contains(_filePath, StringComparer.OrdinalIgnoreCase))
                settings.PinnedNotes.Add(_filePath);
        }
        else
        {
            settings.PinnedNotes.RemoveAll(p => p.Equals(_filePath, StringComparison.OrdinalIgnoreCase));
        }
        SettingsService.SaveSettings(settings);
        _refreshParent?.Invoke();
        ToastNotificationHelper.ShowSuccess(_pin ? "Pinned" : "Unpinned");
        return CommandResult.GoBack();
    }
}

internal static class ToastNotificationHelper
{
    // NOTE: These are intentionally stubs that log to Debug output. The Command
    // Palette SDK does not expose a reliable toast API from the extension host; do
    // not "fix" these by adding Windows.UI.Notifications calls without verifying
    // the host context — the extension runs out-of-proc and may not have a toast
    // activator registered. Keep as debug logging until a host-supported API exists.
    public static void ShowSuccess(string message)
    {
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
