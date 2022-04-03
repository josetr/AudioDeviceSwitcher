// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using AudioDeviceSwitcher.Interop;
using System.Runtime.InteropServices;
using static PInvoke.User32;

public sealed class WinNotifyIcon
{
    internal const WindowMessage NotifyIconCallbackId = WindowMessage.WM_USER + 1000;
    private readonly IntPtr hwnd;
    private readonly string title;
    private readonly IntPtr iconHandle;
    private Shell32.NOTIFYICONDATA notifyIconData;
    private bool created;

    public WinNotifyIcon(IntPtr hwnd, string title, IntPtr iconHandle)
    {
        this.hwnd = hwnd;
        this.title = title;
        this.iconHandle = iconHandle;
    }

    public void Create()
    {
        if (created)
            return;

        created = true;
        notifyIconData = new()
        {
            hWnd = hwnd,
            uID = 10000,
            szTip = title,
            hIcon = iconHandle,
            cbSize = Marshal.SizeOf(notifyIconData),
            uFlags = Shell32.NIF_ICON | Shell32.NIF_MESSAGE | Shell32.NIF_TIP,
            uCallbackMessage = (int)NotifyIconCallbackId,
        };
        Shell32.Shell_NotifyIcon(Shell32.NIM_ADD, in notifyIconData);
    }

    public void OnTaskBarCreated()
    {
        created = false;
        Create();
    }

    public void Delete()
    {
        if (!created)
            return;

        Shell32.Shell_NotifyIcon(Shell32.NIM_DELETE, in notifyIconData);
        created = false;
    }
}
