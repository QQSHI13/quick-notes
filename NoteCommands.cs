// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
