// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using AudioDeviceSwitcher.Interop.UI;
using PUser32 = PInvoke.User32;

internal sealed class WinMenu
{
    private readonly List<WinMenuItem?> options = new();
    private readonly IntPtr hwnd;

    public WinMenu(IntPtr hwnd)
    {
        this.hwnd = hwnd;
    }

    public bool IsEmpty => options.Count == 0;

    public void Add(WinMenuItem? title)
    {
        options.Add(title);
    }

    public void Track()
    {
        PUser32.SetForegroundWindow(hwnd);
        var menu = User32.CreatePopupMenu();

        for (var i = 0; i < options.Count; ++i)
        {
            var option = options[i];
            if (option == null)
            {
                PUser32.AppendMenu(menu, PUser32.MenuItemFlags.MF_SEPARATOR, (IntPtr)(i + 1), null);
                continue;
            }

            var flags = PUser32.MenuItemFlags.MF_STRING;
            flags |= option.Checked ? PUser32.MenuItemFlags.MF_CHECKED : PUser32.MenuItemFlags.MF_UNCHECKED;
            if (option.Disabled)
                flags |= PUser32.MenuItemFlags.MF_DISABLED;
            PUser32.AppendMenu(menu, flags, (IntPtr)(i + 1), option.Title);
        }

        PUser32.GetCursorPos(out var point);
        var index = User32.TrackPopupMenuEx(menu, User32.TPM_RIGHTALIGN | User32.TPM_RETURNCMD, point.x, point.y, hwnd, default) - 1;
        User32.DestroyMenu(menu);

        if (index >= 0 && index < options.Count)
            options[index]?.Callback.Invoke();
    }
}

public sealed record WinMenuItem(string Title, Action Callback, bool Checked = false, bool Disabled = false);
