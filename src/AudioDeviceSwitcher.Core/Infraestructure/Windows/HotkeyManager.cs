// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using AudioDeviceSwitcher.Interop;

public sealed class HotkeyManager : IHotkeyManager
{
    private readonly IntPtr hwnd;

    public HotkeyManager(IntPtr hwnd)
    {
        this.hwnd = hwnd;
    }

    public void RegisterHotkey(int id, Hotkey hotkey)
    {
        if (hotkey != null)
            User32.RegisterHotKey(hwnd, id, hotkey.Modifiers.ToModifiers(), (uint)hotkey.Key);
    }

    public void UnregisterHotkey(int id)
    {
        while (User32.UnregisterHotKey(hwnd, id))
        {
        }
    }
}
