// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using AudioDeviceSwitcher.Interop;
using System;
using static AudioDeviceSwitcher.Interop.User32;

public static partial class Mapping
{
    public static Modifiers ToModifiers(this KeyModifiers modifiers)
    {
        Modifiers result = 0;

        if (modifiers.HasFlag(KeyModifiers.Control))
            result |= Modifiers.MOD_CONTROL;

        if (modifiers.HasFlag(KeyModifiers.Menu))
            result |= Modifiers.MOD_ALT;

        if (modifiers.HasFlag(KeyModifiers.Shift))
            result |= Modifiers.MOD_SHIFT;

        if (modifiers.HasFlag(KeyModifiers.Windows))
            result |= Modifiers.MOD_WIN;

        return result;
    }

    public static AudioDeviceState ToState(this MMDeviceState state)
    {
        return (AudioDeviceState)state;
    }

    public static ERole ToComRole(this AudioDeviceRoleType role)
    {
        return role switch
        {
            AudioDeviceRoleType.Default => ERole.eConsole,
            AudioDeviceRoleType.Communications => ERole.eCommunications,
            _ => throw new NotImplementedException(),
        };
    }

    public static EDataFlow ToComDataFlow(this AudioDeviceClass role)
    {
        return role switch
        {
            AudioDeviceClass.Render => EDataFlow.eRender,
            AudioDeviceClass.Capture => EDataFlow.eCapture,
            _ => throw new NotImplementedException(),
        };
    }
}
