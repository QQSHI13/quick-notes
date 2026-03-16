// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

public partial class QuickNotesCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public QuickNotesCommandsProvider()
    {
        // Log when extension loads
        var logPath = Path.Combine(Path.GetTempPath(), "quicknotes_debug.log");
        try
        {
            File.AppendAllText(logPath, $"\n[{DateTime.Now}] EXTENSION LOADED - Constructor called\n");
            DisplayName = "Quick Notes Extension";
            Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
            _commands = [
                new CommandItem(new QuickNotesPage()) { Title = DisplayName },
            ];
            File.AppendAllText(logPath, $"[{DateTime.Now}] Commands initialized successfully\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR in constructor: {ex}\n");
            DisplayName = "Quick Notes Extension (Error)";
            _commands = Array.Empty<ICommandItem>();
        }
    }

    public override ICommandItem[] TopLevelCommands()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "quicknotes_debug.log");
        File.AppendAllText(logPath, $"[{DateTime.Now}] TopLevelCommands called\n");
        return _commands;
    }
}
