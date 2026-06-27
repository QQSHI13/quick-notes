// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

#nullable enable

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

internal sealed partial class EditorConfigurationPage : ListPage
{
    public EditorConfigurationPage()
    {
        Icon = new IconInfo(new IconData("\uE70A")); // Edit icon
        Title = "Configure Editor";
        Name = "Configure Editor";
    }

    public override IListItem[] GetItems()
    {
        var settings = SettingsService.GetSettings();
        var currentEditor = settings.DefaultEditor ?? "notepad.exe";

        var items = new System.Collections.Generic.List<IListItem>
        {
            // Show current editor (non-selectable)
            new ListItem(new NoOpCommand())
            {
                Title = "Current Editor",
                Subtitle = currentEditor,
                Icon = new IconInfo(new IconData("\uE70A")),
            },
        };

        // Auto-detected installed editors (full paths). These are more reliable
        // than relying on a command being on PATH.
        foreach (var (label, path) in EditorDetection.DetectInstalledEditors())
        {
            items.Add(new ListItem(new SetDefaultEditorCommand(path, () => RaiseItemsChanged()))
            {
                Title = $"Use {label}",
                Subtitle = path,
                Icon = new IconInfo(new IconData("\uE7C3")),
            });
        }

        // Quick presets (command names resolved via PATH / always-available)
        items.Add(new ListItem(new SetDefaultEditorCommand("notepad.exe", () => RaiseItemsChanged()))
        {
            Title = "Use Notepad",
            Subtitle = "Windows default text editor",
            Icon = new IconInfo(new IconData("\uE8A5")),
        });
        items.Add(new ListItem(new SetDefaultEditorCommand("wordpad", () => RaiseItemsChanged()))
        {
            Title = "Use WordPad",
            Subtitle = "Windows WordPad",
            Icon = new IconInfo(new IconData("\uE8A5")),
        });

        // For anything else, edit settings.json directly.
        items.Add(new ListItem(new EditSettingsCommand())
        {
            Title = "Set Custom Path…",
            Subtitle = "Open settings.json and set defaultEditor to any .exe",
            Icon = new IconInfo(new IconData("\uE70F")),
        });

        return items.ToArray();
    }
}

internal static class EditorDetection
{
    // Returns (display label, full path) for editors found on the machine.
    public static System.Collections.Generic.List<(string Label, string Path)> DetectInstalledEditors()
    {
        // (label, relative path under an install root)
        var candidates = new (string Label, string RelativePath)[]
        {
            ("VS Code", @"Microsoft VS Code\Code.exe"),
            ("VS Code Insiders", @"Microsoft VS Code Insiders\Code - Insiders.exe"),
            ("Obsidian", @"Obsidian\Obsidian.exe"),
            ("Notepad++", @"Notepad++\notepad++.exe"),
            ("Typora", @"Typora\Typora.exe"),
            ("Sublime Text", @"Sublime Text\sublime_text.exe"),
            ("Cursor", @"Cursor\Cursor.exe"),
        };

        // VS Code/Cursor live under %LOCALAPPDATA%\Programs; Notepad++/Typora/Sublime
        // typically under Program Files. Search all roots.
        var roots = new System.Collections.Generic.List<string>();
        var la = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(la))
        {
            roots.Add(System.IO.Path.Combine(la, "Programs"));
            roots.Add(la);
        }
        foreach (var pf in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        })
        {
            if (!string.IsNullOrEmpty(pf)) roots.Add(pf);
        }

        var found = new System.Collections.Generic.List<(string, string)>();
        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots)
        {
            foreach (var (label, rel) in candidates)
            {
                var path = System.IO.Path.Combine(root, rel);
                if (System.IO.File.Exists(path) && seen.Add(path))
                {
                    found.Add((label, path));
                }
            }
        }
        return found;
    }
}

public sealed partial class SetDefaultEditorCommand : InvokableCommand
{
    private readonly string _editorPath;
    private readonly Action? _refreshParent;

    public SetDefaultEditorCommand(string editorPath, Action? refreshParent = null)
    {
        _editorPath = editorPath ?? throw new ArgumentNullException(nameof(editorPath));
        _refreshParent = refreshParent;
    }

    public override ICommandResult Invoke()
    {
        try
        {
            var settings = SettingsService.GetSettings();
            settings.DefaultEditor = _editorPath;
            SettingsService.SaveSettings(settings);

            ToastNotificationHelper.ShowSuccess($"Editor set to: {_editorPath}");
            // Refresh the editor page so the new "Current Editor" shows.
            _refreshParent?.Invoke();
        }
        catch (Exception ex)
        {
            ToastNotificationHelper.ShowError($"Failed to set editor: {ex.Message}");
        }

        return CommandResult.GoBack();
    }
}

