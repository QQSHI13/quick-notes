// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

internal sealed partial class EditorConfigurationPage : ListPage
{
    private string _editorPath = string.Empty;

    public EditorConfigurationPage()
    {
        Icon = new IconInfo(new IconData("\uE70A")); // Edit icon
        Title = "Configure Editor";
        Name = "Configure Editor";
    }

    // Called when user types in the search box - override to capture input
    public override void UpdateQuery(string query)
    {
        _editorPath = query?.Trim() ?? string.Empty;
    }

    public override IListItem[] GetItems()
    {
        var settings = SettingsService.GetSettings();
        var currentEditor = settings.DefaultEditor ?? "notepad.exe";

        var items = new System.Collections.Generic.List<IListItem>();

        // Show current editor
        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "Current Editor",
            Subtitle = currentEditor,
            Icon = new IconInfo(new IconData("\uE70A")),
        });

        items.Add(new ListItem(new SetDefaultEditorCommand("notepad.exe"))
        {
            Title = "Use Notepad",
            Subtitle = "Windows default text editor",
            Icon = new IconInfo(new IconData("\uE8A5")),
        });

        items.Add(new ListItem(new SetDefaultEditorCommand("code"))
        {
            Title = "Use VS Code",
            Subtitle = "Visual Studio Code (if installed)",
            Icon = new IconInfo(new IconData("\uE7C3")),
        });

        items.Add(new ListItem(new SetDefaultEditorCommand("notepad++"))
        {
            Title = "Use Notepad++",
            Subtitle = "Notepad++ (if installed)",
            Icon = new IconInfo(new IconData("\uE8A5")),
        });

        // If user typed a path, show option to use it
        if (!string.IsNullOrWhiteSpace(_editorPath))
        {
            var isValidPath = IsValidEditorPath(_editorPath);
            items.Add(new ListItem(new SetDefaultEditorCommand(_editorPath))
            {
                Title = $"Use: {_editorPath}",
                Subtitle = isValidPath ? "Click to set as default editor" : "Warning: Path may be invalid",
                Icon = new IconInfo(new IconData(isValidPath ? "\uE73E" : "\uE711")),
            });
        }

        // Instructions for setting a custom path
        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "Set Custom Editor",
            Subtitle = "Type the full path to your editor above",
            Icon = new IconInfo(new IconData("\uE8B7")),
        });

        // Instructions
        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "Instructions",
            Subtitle = "Type a full path to an .exe file, or use a command in PATH",
            Icon = new IconInfo(new IconData("\uE897")),
        });

        return items.ToArray();
    }

    private static bool IsValidEditorPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // If it's just a command name (no path separators), assume it's valid
        if (!path.Contains(Path.DirectorySeparatorChar) && !path.Contains('/'))
            return true;

        try
        {
            // Check if file exists
            return File.Exists(path) && 
                   (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}

public sealed partial class SetDefaultEditorCommand : InvokableCommand
{
    private readonly string _editorPath;

    public SetDefaultEditorCommand(string editorPath)
    {
        _editorPath = editorPath ?? throw new ArgumentNullException(nameof(editorPath));
    }

    public override ICommandResult Invoke()
    {
        try
        {
            var settings = SettingsService.GetSettings();
            settings.DefaultEditor = _editorPath;
            SettingsService.SaveSettings(settings);

            ToastNotificationHelper.ShowSuccess($"Editor set to: {_editorPath}");
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to set editor: {ex.Message}");
        }

        return CommandResult.GoBack();
    }
}

