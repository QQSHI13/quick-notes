// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace QuickNotes;

public partial class QuickNotesCommandsProvider : CommandProvider, IDisposable
{
    private readonly ICommandItem[] _commands;
    private readonly QuickNotesPage _quickNotesPage;

    public QuickNotesCommandsProvider()
    {
        DisplayName = "Quick Notes Extension";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _quickNotesPage = new QuickNotesPage();
        _commands = [
            new CommandItem(_quickNotesPage) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

    public override void Dispose()
    {
        _quickNotesPage?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
