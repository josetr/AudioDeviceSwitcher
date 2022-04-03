// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public delegate void AudioEventDelegate(object evt);

public sealed class AudioEvents
{
    public event AudioEventDelegate? Events;

    public void Raise(object msg)
    {
        Events?.Invoke(msg);
    }
}
