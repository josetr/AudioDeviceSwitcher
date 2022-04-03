// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Text.Json;

public sealed class StateStorage : IStateStorage
{
    public AudioSwitcherState Load()
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        var content = localSettings.Values["settings"]?.ToString();
        return string.IsNullOrWhiteSpace(content) ? new() : (JsonSerializer.Deserialize<AudioSwitcherState>(content) ?? new());
    }

    public void Save(AudioSwitcherState state)
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        localSettings.Values["settings"] = JsonSerializer.Serialize(state);
    }
}
