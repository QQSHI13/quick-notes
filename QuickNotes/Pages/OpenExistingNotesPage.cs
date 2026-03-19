// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

internal sealed partial class OpenExistingNotesPage : ListPage, IDisposable
{
    private FileSystemWatcher? _watcher;
    private DateTime _lastRefresh = DateTime.MinValue;
    private static readonly TimeSpan _refreshCooldown = TimeSpan.FromSeconds(1);
    private bool _disposed;

    public OpenExistingNotesPage()
    {
        Icon = new IconInfo(new IconData("\uE8E5")); // Open folder icon
        Title = "Open Existing Notes";
        Name = "Open Existing";
        
        SetupFileSystemWatcher();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _watcher?.Dispose();
        _watcher = null;
    }

    private void SetupFileSystemWatcher()
    {
        try
        {
            // Clean up old watcher if directory changed
            _watcher?.Dispose();

            var settings = SettingsService.GetSettings();
            var notesDir = settings.NotesDirectory ?? PathHelper.GetDefaultNotesDirectory();

            if (!Directory.Exists(notesDir))
            {
                return;
            }

            _watcher = new FileSystemWatcher(notesDir, "*.md")
            {
                NotifyFilter = NotifyFilters.FileName | 
                               NotifyFilters.LastWrite | 
                               NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            // Throttle refreshes to avoid excessive updates
            _watcher.Created += (s, e) => RequestRefresh();
            _watcher.Deleted += (s, e) => RequestRefresh();
            _watcher.Renamed += (s, e) => RequestRefresh();
            _watcher.Changed += (s, e) => RequestRefresh();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FILE WATCHER] Error setting up watcher: {ex.Message}");
        }
    }

    private void RequestRefresh()
    {
        var now = DateTime.Now;
        if (now - _lastRefresh < _refreshCooldown)
        {
            return;
        }
        _lastRefresh = now;

        // The Command Palette will refresh when the user navigates
        // We can't force a refresh programmatically, but the next GetItems() call
        // will return updated data
    }

    public override IListItem[] GetItems()
    {
        // Ensure watcher is set up (in case directory changed)
        SetupFileSystemWatcher();

        var settings = SettingsService.GetSettings();
        var notesDir = settings.NotesDirectory ?? PathHelper.GetDefaultNotesDirectory();

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
        var items = noteFiles
            .OrderByDescending(f => f.LastModified)
            .Select(f =>
            {
                var command = new OpenNoteCommand(f.FullPath);
                var syncCommand = new SyncNoteTitleCommand(f.FullPath);
                var deleteCommand = new ConfirmDeleteNoteCommand(f.FullPath);

                return new ListItem(command)
                {
                    Title = string.IsNullOrEmpty(f.Title) ? f.Name : f.Title,
                    Subtitle = $"{f.Name} • Modified: {f.LastModified:yyyy-MM-dd HH:mm}",
                    Icon = new IconInfo(new IconData("\uE8A5")), // Document icon
                    MoreCommands = new[]
                    {
                        new CommandContextItem(syncCommand)
                        {
                            Title = "Sync Title",
                            Icon = new IconInfo(new IconData("\uE8AC")),
                        },
                        new CommandContextItem(deleteCommand)
                        {
                            Title = "Delete",
                            Icon = new IconInfo(new IconData("\uE74D")),
                        },
                    }
                };
            })
            .ToList<IListItem>();

        return items.ToArray();
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
                    
                    // Skip files that can't be read
                    if (!fileInfo.Exists)
                        continue;

                    var title = ExtractTitleFromFile(file);
                    
                    notes.Add(new NoteFile
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        LastModified = fileInfo.LastWriteTime,
                        Title = title,
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GET NOTES] Error reading file {file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GET NOTES] Error accessing directory {directory}: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EXTRACT TITLE] Error reading file {filePath}: {ex.Message}");
        }
        
        return null;
    }

    private sealed class NoteFile
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string? Title { get; set; }
    }
}

internal sealed partial class DeleteConfirmationPage : ListPage
{
    private readonly string _filePath;
    private readonly string _fileName;

    public DeleteConfirmationPage(string filePath, string fileName)
    {
        _filePath = filePath;
        _fileName = fileName;
        Icon = new IconInfo(new IconData("\uE74D")); // Delete icon
        Title = "Confirm Delete";
        Name = "Confirm Delete";
    }

    public override IListItem[] GetItems()
    {
        return new[]
        {
            new ListItem(new DeleteNoteCommand(_filePath))
            {
                Title = $"Delete '{_fileName}'",
                Subtitle = "This action cannot be undone",
                Icon = new IconInfo(new IconData("\uE74D")), // Delete icon
            },
            new ListItem(new GoBackCommand())
            {
                Title = "Cancel",
                Subtitle = "Keep the note",
                Icon = new IconInfo(new IconData("\uE711")), // Cancel icon
            },
        };
    }
}

public sealed partial class GoBackCommand : InvokableCommand
{
    public GoBackCommand()
    {
        Icon = new IconInfo(new IconData("\uE72B")); // Back icon
    }

    public override ICommandResult Invoke()
    {
        return CommandResult.GoBack();
    }
}
