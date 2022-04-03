// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Microsoft.UI.Dispatching;

public sealed class WinDispatcher : IDispatcher
{
    private readonly DispatcherQueue dispatcher;

    public WinDispatcher(DispatcherQueue dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    public void Enqueue(Action action)
    {
        dispatcher.TryEnqueue(() => action());
    }
}
