// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public interface IAudioManager
{
    Task<AudioDevice[]> GetAllDevicesAsync(AudioDeviceClass type);
    string GetDefaultAudioId(AudioDeviceClass type, AudioDeviceRoleType role);
    bool IsDefault(string id, AudioDeviceClass type, AudioDeviceRoleType role);
    bool SetDefaultDevice(string id, AudioDeviceRoleType role);
    void SetVisibility(string id, bool visible);
    AudioDeviceState GetState(string id);
    bool IsDisabled(string id) => GetState(id).HasFlag(AudioDeviceState.Disabled);
    bool IsActive(string id) => GetState(id).HasFlag(AudioDeviceState.Active);
}
