// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

internal sealed partial class QuickNotesPage : ListPage
{
    public QuickNotesPage()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "quicknotes_debug.log");
        try
        {
            File.AppendAllText(logPath, $"\n[{DateTime.Now}] QuickNotesPage created\n");
            Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
            Title = "Quick Notes Extension";
            Name = "Open";
            File.AppendAllText(logPath, $"[{DateTime.Now}] QuickNotesPage initialized OK\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR in QuickNotesPage ctor: {ex}\n");
        }
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
                new ListItem(new CreateNewNoteCommand()) 
                { 
                    Title = "Create New", 
                    Subtitle = "Create a new markdown note",
                    Icon = new IconInfo(new IconData("\uE710")), // Add icon
                },
                new ListItem(new OpenExistingNotesPage()) 
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
            System.Diagnostics.Debug.WriteLine($"[QUICKNOTES] Error in GetItems: {ex}");
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
