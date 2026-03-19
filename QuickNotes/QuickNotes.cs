// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
