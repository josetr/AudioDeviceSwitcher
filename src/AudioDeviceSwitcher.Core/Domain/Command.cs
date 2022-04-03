// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public sealed record Command
{
    public Command(CommandName name, AudioDeviceClass type, Hotkey? hotkey = null, string[]? devices = null)
    {
        Name = name;
        DeviceClass = type;
        if (hotkey != null)
            Hotkey = hotkey;
        if (devices != null)
            Devices = devices;
    }

    public Command()
    {
    }

    public string Name { get; init; } = string.Empty;
    public CommandType Action { get; init; }
    public AudioDeviceClass DeviceClass { get; init; }
    public int HotkeyId => (int)Name.GetDjb2HashCode();
    public Hotkey Hotkey { get; init; } = new();
    public string[] Devices { get; init; } = Array.Empty<string>();
}
