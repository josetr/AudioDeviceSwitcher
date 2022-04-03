// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public sealed record AudioDeviceAdded(string Id, AudioDevice Device);
public sealed record AudioDeviceRemoved(string Id);
public sealed record AudioDeviceUpdated(string Id, AudioDeviceChanges Changes);
public sealed record DefaultAudioDeviceChanged(AudioDeviceRoleType Role, string Id);
