// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1310 // Field names should not contain underscore

namespace AudioDeviceSwitcher.Interop;

using System;
using System.Runtime.InteropServices;
using Windows.System;

public partial class User32
{
    public enum Modifiers
    {
        MOD_ALT = 1,
        MOD_CONTROL = 2,
        MOD_SHIFT = 4,
        MOD_WIN = 8,
        MOD_NOREPEAT = 0x4000,
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, Modifiers fsModifiers, VirtualKey vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public static Modifiers ToModifiers(VirtualKeyModifiers modifiers)
    {
        Modifiers result = 0;

        if (modifiers.HasFlag(VirtualKeyModifiers.Control))
            result |= Modifiers.MOD_CONTROL;

        if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
            result |= Modifiers.MOD_ALT;

        if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
            result |= Modifiers.MOD_SHIFT;

        if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
            result |= Modifiers.MOD_WIN;

        return result;
    }

    public static VirtualKeyModifiers ToVirtualKeyModifiers(Modifiers modifiers)
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
}
