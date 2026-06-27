// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace QuickNotes;

[Guid("2e9692a6-4e82-4eb8-85c7-a8d3cede95de")]
public sealed partial class QuickNotes : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly QuickNotesCommandsProvider _provider;

    public QuickNotes(ManualResetEvent extensionDisposedEvent)
    {
        this._extensionDisposedEvent = extensionDisposedEvent;
        this._provider = new QuickNotesCommandsProvider();
    }

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    public void Dispose()
    {
        _provider?.Dispose();
        this._extensionDisposedEvent.Set();
    }
}
