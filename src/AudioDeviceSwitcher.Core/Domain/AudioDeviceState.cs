// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

[Flags]
public enum AudioDeviceState
{
    Invalid = 0,
    Active = 1,
    Disabled = 2,
    NotPresent = 4,
    Unplugged = 8,
    All = 16,
}
