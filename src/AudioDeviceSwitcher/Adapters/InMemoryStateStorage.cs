// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public sealed class InMemoryStateStorage : IStateStorage
{
    private AudioSwitcherState state = new();

    public AudioSwitcherState Load()
    {
        return state;
    }

    public void Save(AudioSwitcherState state)
    {
        this.state = state;
    }
}
