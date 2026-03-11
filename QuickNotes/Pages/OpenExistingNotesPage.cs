// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

internal sealed partial class OpenExistingNotesPage : ListPage
{
    public OpenExistingNotesPage()
    {
        Icon = new IconInfo(new IconData("\uE8E5")); // Open folder icon
        Title = "Open Existing Notes";
        Name = "Open Existing";
    }

    public override IListItem[] GetItems()
    {
        var settings = SettingsService.GetSettings();
        var notesDir = settings.NotesDirectory ?? GetDefaultNotesDirectory();

        if (!Directory.Exists(notesDir))
        {
            return
            [
                new ListItem(new NoOpCommand()) 
                { 
                    Title = "No notes found", 
                    Subtitle = $"Directory does not exist: {notesDir}",
                    Icon = new IconInfo(new IconData("\uE711")), // Error/Warning icon
                },
                new ListItem(new CreateNewNoteCommand()) 
                { 
                    Title = "Create your first note", 
                    Subtitle = "Click to create a new note",
                    Icon = new IconInfo(new IconData("\uE710")), // Add icon
                },
            ];
        }

        var noteFiles = GetNoteFiles(notesDir);

        if (noteFiles.Count == 0)
        {
            return
            [
                new ListItem(new NoOpCommand()) 
                { 
                    Title = "No notes found", 
                    Subtitle = "No .md files in your notes directory",
                    Icon = new IconInfo(new IconData("\uE711")), // Error/Warning icon
                },
                new ListItem(new CreateNewNoteCommand()) 
                { 
                    Title = "Create your first note", 
                    Subtitle = "Click to create a new note",
                    Icon = new IconInfo(new IconData("\uE710")), // Add icon
                },
            ];
        }

        // Sort by last modified (newest first) and create list items
        return noteFiles
            .OrderByDescending(f => f.LastModified)
            .Select(f => new ListItem(new OpenNoteCommand(f.FullPath))
            {
                Title = string.IsNullOrEmpty(f.Title) ? f.Name : f.Title,
                Subtitle = $"{f.Name} • Modified: {f.LastModified:yyyy-MM-dd HH:mm}",
                Icon = new IconInfo(new IconData("\uE8A5")), // Document icon
            })
            .ToArray<IListItem>();
    }

    private static List<NoteFile> GetNoteFiles(string directory)
    {
        var notes = new List<NoteFile>();

        try
        {
            var mdFiles = Directory.GetFiles(directory, "*.md", SearchOption.TopDirectoryOnly);

            foreach (var file in mdFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var title = ExtractTitleFromFile(file);
                    
                    notes.Add(new NoteFile
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        LastModified = fileInfo.LastWriteTime,
                        Title = title,
                    });
                }
                catch
                {
                    // Skip files we can't access
                }
            }
        }
        catch
        {
            // Return empty list if we can't access directory
        }

        return notes;
    }

    private static string? ExtractTitleFromFile(string filePath)
    {
        try
        {
            // Read first few lines to find the title
            var lines = File.ReadLines(filePath).Take(10);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Look for markdown heading: # Title or #Title
                if (trimmed.StartsWith("# ", StringComparison.Ordinal))
                {
                    return trimmed.Substring(2).Trim();
                }
                if (trimmed.StartsWith('#'))
                {
                    var title = trimmed.TrimStart('#').Trim();
                    if (!string.IsNullOrEmpty(title))
                    {
                        return title;
                    }
                }
            }
        }
        catch
        {
            // Ignore errors reading file
        }
        
        return null;
    }

    private static string GetDefaultNotesDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "QuickNotes");
    }

    private sealed class NoteFile
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string? Title { get; set; }
    }
}
