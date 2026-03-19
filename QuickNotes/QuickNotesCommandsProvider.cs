// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    public void Dispose()
    {
        _quickNotesPage?.Dispose();
    }
}
