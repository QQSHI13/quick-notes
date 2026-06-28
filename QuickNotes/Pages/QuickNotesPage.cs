// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

internal sealed partial class QuickNotesPage : ListPage, IDisposable
{
    private readonly OpenExistingNotesPage _openExistingPage;
    private bool _disposed;

    public QuickNotesPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Quick Notes Extension";
        Name = "Open";
        _openExistingPage = new OpenExistingNotesPage();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _openExistingPage?.Dispose();
    }

    public override IListItem[] GetItems()
    {
        try
        {
            var settings = SettingsService.GetSettings();
            var notesDir = settings.NotesDirectory ?? GetDefaultNotesDirectory();
            
            // Check if there are any notes to sync
            var hasNotes = Directory.Exists(notesDir) && Directory.GetFiles(notesDir, "*.md").Length > 0;

            var items = new List<IListItem>
            {
                new ListItem(new CreateNewPage()) 
                { 
                    Title = "Create New", 
                    Subtitle = "Choose a template or create a blank note",
                    Icon = new IconInfo(new IconData("\uE710")), // Add icon
                },
                new ListItem(_openExistingPage) 
                { 
                    Title = "Open Existing", 
                    Subtitle = "Browse and open existing notes",
                    Icon = new IconInfo(new IconData("\uE8E5")), // Open folder icon
                },
            };

            // Add Sync All Titles option if there are notes
            if (hasNotes)
            {
                items.Add(new ListItem(new SyncAllNoteTitlesCommand())
                {
                    Title = "Sync All Titles",
                    Subtitle = "Rename all notes to match their headings",
                    Icon = new IconInfo(new IconData("\uE8AC")), // Sync icon
                });
            }

            items.Add(new ListItem(new SettingsPage()) 
            { 
                Title = "Settings", 
                Subtitle = "Configure notes directory",
                Icon = new IconInfo(new IconData("\uE713")), // Settings icon
            });

            return items.ToArray();
        }
        catch (Exception ex)
        {
            // Return error item so user knows something went wrong
            return new[]
            {
                new ListItem(new NoOpCommand())
                {
                    Title = "Error loading extension",
                    Subtitle = ex.Message,
                    Icon = new IconInfo(new IconData("\uE711")), // Error icon
                }
            };
        }
    }

    private static string GetDefaultNotesDirectory()
    {
        return Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
            "QuickNotes");
    }
}
