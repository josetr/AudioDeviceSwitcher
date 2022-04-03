// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1310 // Field names should not contain underscore

namespace AudioDeviceSwitcher.Interop;

using System.Runtime.InteropServices;

public sealed partial class User32
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
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, Modifiers fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
