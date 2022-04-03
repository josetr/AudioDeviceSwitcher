// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System;
using Windows.Media.Devices;
using Windows.System;
using static AudioDeviceSwitcher.Interop.User32;

public static partial class Mapping
{
    public static AudioDeviceRoleType ToRole(this AudioDeviceRole role)
    {
        return role switch
        {
            AudioDeviceRole.Default => AudioDeviceRoleType.Default,
            AudioDeviceRole.Communications => AudioDeviceRoleType.Communications,
            _ => throw new NotImplementedException(),
        };
    }

    public static AudioDeviceRole ToRole(this AudioDeviceRoleType role)
    {
        return role switch
        {
            AudioDeviceRoleType.Default => AudioDeviceRole.Default,
            AudioDeviceRoleType.Communications => AudioDeviceRole.Communications,
            _ => throw new NotImplementedException(),
        };
    }

    public static VirtualKey ToVirtualKey(this Key key)
    {
        return (VirtualKey)key;
    }

    public static VirtualKeyModifiers ToVirtualKeyModifiers(this KeyModifiers key)
    {
        return (VirtualKeyModifiers)key;
    }

    public static VirtualKeyModifiers ToVirtualKeyModifiers(this Modifiers modifiers)
    {
        VirtualKeyModifiers result = 0;
        if (modifiers.HasFlag(Modifiers.MOD_CONTROL))
            result |= VirtualKeyModifiers.Control;

        if (modifiers.HasFlag(Modifiers.MOD_ALT))
            result |= VirtualKeyModifiers.Menu;

        if (modifiers.HasFlag(Modifiers.MOD_SHIFT))
            result |= VirtualKeyModifiers.Shift;

        if (modifiers.HasFlag(Modifiers.MOD_WIN))
            result |= VirtualKeyModifiers.Windows;

        return result;
    }

    public static KeyModifiers ToKeyModifiers(this VirtualKeyModifiers key)
    {
        return (KeyModifiers)key;
    }

    public static Key ToKey(this VirtualKey key)
    {
        return (Key)key;
    }
}
