// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

/// <summary>
/// Sub-page shown when the user clicks "Create New". Lists available templates
/// from <c>notesDirectory\_templates\</c> plus a "Blank note" option.
/// </summary>
internal sealed partial class CreateNewPage : ListPage
{
    public CreateNewPage()
    {
        Icon = new IconInfo(new IconData("\uE710")); // Add icon
        Title = "Create New";
        Name = "Create New";
    }

    public override IListItem[] GetItems()
    {
        var settings = SettingsService.GetSettings();
        var notesDir = settings.NotesDirectory ?? PathHelper.GetDefaultNotesDirectory();
        var templatesDir = Path.Combine(notesDir, "_templates");

        var items = new List<IListItem>();

        // 1. Blank note (no template)
        items.Add(new ListItem(new CreateNewNoteCommand(null, () => RaiseItemsChanged()))
        {
            Title = "Blank note",
            Subtitle = "Simple timestamped markdown note",
            Icon = new IconInfo(new IconData("\uE710")),
        });

        // 2. Discovered templates
        if (Directory.Exists(templatesDir))
        {
            foreach (var file in Directory.GetFiles(templatesDir, "*.md"))
            {
                try
                {
                    var tplName = Path.GetFileNameWithoutExtension(file);
                    var tplContent = File.ReadAllText(file);
                    items.Add(new ListItem(new CreateNewNoteCommand(tplContent, () => RaiseItemsChanged()))
                    {
                        Title = tplName,
                        Subtitle = $"Use template: {tplName}",
                        Icon = new IconInfo(new IconData("\uE710")),
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TEMPLATE] Error reading {file}: {ex.Message}");
                }
            }
        }

        // 3. Default template shortcut (if one is configured)
        if (!string.IsNullOrEmpty(settings.Template))
        {
            try
            {
                if (File.Exists(settings.Template))
                {
                    var tplContent = File.ReadAllText(settings.Template);
                    var tplName = Path.GetFileNameWithoutExtension(settings.Template);
                    items.Add(new ListItem(new CreateNewNoteCommand(tplContent, () => RaiseItemsChanged())
                    {
                        // also set a title so the parameter gets consumed
                    })
                    {
                        Title = "Default template",
                        Subtitle = tplName,
                        Icon = new IconInfo(new IconData("\uE710")),
                    });
                }
            }
            catch { }
        }

        return items.ToArray();
    }
}