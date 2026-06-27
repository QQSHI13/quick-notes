// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

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

        EnsureWatcherForCurrentDirectory();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _watcher?.Dispose();
        _watcher = null;
    }

    private string? _watchedDirectory;

    private void EnsureWatcherForCurrentDirectory()
    {
        try
        {
            var settings = SettingsService.GetSettings();
            var notesDir = settings.NotesDirectory ?? PathHelper.GetDefaultNotesDirectory();

            // Already watching this exact directory? Nothing to do.
            if (string.Equals(_watchedDirectory, notesDir, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _watcher?.Dispose();
            _watcher = null;
            _watchedDirectory = null;

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
            _watchedDirectory = notesDir;
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

        // The Command Palette host does not expose a programmatic refresh trigger
        // for list pages, so we cannot push updates to the view. The watcher is
        // kept alive only so that the next GetItems() call reflects the current
        // on-disk state (GetItems re-reads the directory every time it is called).
        // If a host-side refresh API becomes available, hook it here.
    }

    public override IListItem[] GetItems()
    {
        // Re-ensure the watcher points at the currently configured directory.
        // EnsureWatcherForCurrentDirectory is a no-op if the directory hasn't changed.
        EnsureWatcherForCurrentDirectory();

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
                var syncCommand = new SyncNoteTitleCommand(f.FullPath, () => RaiseItemsChanged());
                var deletePage = new DeleteConfirmationPage(f.FullPath, f.Name, () => RaiseItemsChanged());

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
                        new CommandContextItem(deletePage)
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

                    var title = NoteTitleHelper.ExtractTitle(file);
                    
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
    private readonly Action? _refreshParent;

    public DeleteConfirmationPage(string filePath, string fileName, Action? refreshParent = null)
    {
        _filePath = filePath;
        _fileName = fileName;
        _refreshParent = refreshParent;
        Icon = new IconInfo(new IconData("\uE74D")); // Delete icon
        Title = "Confirm Delete";
        Name = "Confirm Delete";
    }

    public override IListItem[] GetItems()
    {
        return new[]
        {
            new ListItem(new DeleteNoteCommand(_filePath, _refreshParent))
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
