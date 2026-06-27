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

        return
        [
            // Show current editor (non-selectable)
            new ListItem(new NoOpCommand())
            {
                Title = "Current Editor",
                Subtitle = currentEditor,
                Icon = new IconInfo(new IconData("\uE70A")),
            },
            new ListItem(new SetDefaultEditorCommand("notepad.exe", () => RaiseItemsChanged()))
            {
                Title = "Use Notepad",
                Subtitle = "Windows default text editor",
                Icon = new IconInfo(new IconData("\uE8A5")),
            },
            new ListItem(new SetDefaultEditorCommand("code", () => RaiseItemsChanged()))
            {
                Title = "Use VS Code",
                Subtitle = "Visual Studio Code (if installed)",
                Icon = new IconInfo(new IconData("\uE7C3")),
            },
            new ListItem(new SetDefaultEditorCommand("notepad++", () => RaiseItemsChanged()))
            {
                Title = "Use Notepad++",
                Subtitle = "Notepad++ (if installed)",
                Icon = new IconInfo(new IconData("\uE8A5")),
            },
            new ListItem(new SetDefaultEditorCommand("obsidian", () => RaiseItemsChanged()))
            {
                Title = "Use Obsidian",
                Subtitle = "Obsidian (if installed)",
                Icon = new IconInfo(new IconData("\uE8A5")),
            },
            new ListItem(new SetDefaultEditorCommand("typora", () => RaiseItemsChanged()))
            {
                Title = "Use Typora",
                Subtitle = "Typora (if installed)",
                Icon = new IconInfo(new IconData("\uE8A5")),
            },
            new ListItem(new SetDefaultEditorCommand("wordpad", () => RaiseItemsChanged()))
            {
                Title = "Use WordPad",
                Subtitle = "Windows WordPad",
                Icon = new IconInfo(new IconData("\uE8A5")),
            },
            // For anything else, edit settings.json directly.
            new ListItem(new EditSettingsCommand())
            {
                Title = "Set Custom Path…",
                Subtitle = "Edit settings.json (defaultEditor field)",
                Icon = new IconInfo(new IconData("\uE70F")),
            },
        ];
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

