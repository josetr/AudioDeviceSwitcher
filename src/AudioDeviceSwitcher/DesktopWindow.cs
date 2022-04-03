// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AudioDeviceSwitcher.Interop.UI;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using static PInvoke.User32;

public class DesktopWindow : Window
{
    private static readonly WindowMessage TaskBarCreatedMsgId = (WindowMessage)RegisterWindowMessage("TaskbarCreated");
    private static readonly WindowMessage RestoreMsgId = (WindowMessage)RegisterWindowMessage(App.Id);
    private static IntPtr _sharedHwnd;
    private readonly IntPtr _hwnd;
    private readonly IntPtr _oldWndProc;
    private readonly WinProc? _newWndProc;
    private readonly WinNotifyIcon _shellIcon;
    private readonly IntPtr _iconHandle;

    public DesktopWindow(string title, string icon)
    {
        _sharedHwnd = _hwnd = WindowNative.GetWindowHandle(this);
        _newWndProc = new WinProc(NewWindowProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, WindowLongIndexFlags.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
        Title = title;
        _iconHandle = SetIcon(icon);
        _shellIcon = new(Hwnd, title, _iconHandle);
    }

    public delegate void HotkeyDelegate(int id, KeyModifiers mod, Windows.System.VirtualKey ukey);
    private delegate IntPtr WinProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

    public static IntPtr SharedHwnd => _sharedHwnd;
    public IntPtr Hwnd => _hwnd;
    public int MinWidth { get; set; } = -1;
    public int MinHeight { get; set; } = -1;
    public bool IsVisible => IsWindowVisible(_hwnd);
    public bool IsActive => GetActiveWindow() == _hwnd;
    protected WinNotifyIcon ShellIcon => _shellIcon;

    public void Maximize() => _ = ShowWindow(_hwnd, WindowShowStyle.SW_MAXIMIZE);
    public void Minimize() => _ = ShowWindow(_hwnd, WindowShowStyle.SW_MINIMIZE);
    public void Restore() => _ = ShowWindow(_hwnd, WindowShowStyle.SW_RESTORE);
    public void Hide() => _ = ShowWindow(_hwnd, WindowShowStyle.SW_HIDE);

    public void SetWindowSize(int width, int height)
    {
        SetWindowPos(_hwnd, SpecialWindowHandles.HWND_TOP, 0, 0, width, height, SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOACTIVATE);
    }

    public IntPtr SetIcon(string name)
    {
        const int ICON_SMALL = 0;
        const int ICON_BIG = 1;
        var small = WinUtil.LoadIcon(name, big: false);
        SendMessage(_hwnd, WindowMessage.WM_SETICON, (IntPtr)ICON_SMALL, small);
        SendMessage(_hwnd, WindowMessage.WM_SETICON, (IntPtr)ICON_BIG, WinUtil.LoadIcon(name, big: true));
        return small;
    }

    public static void BroadcastRestore()
    {
        PostMessage(HWND_BROADCAST, RestoreMsgId, IntPtr.Zero, IntPtr.Zero);
    }

    protected virtual unsafe IntPtr NewWindowProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            switch (msg)
            {
                case WindowMessage.WM_GETMINMAXINFO:
                    ref var minMaxInfo = ref Unsafe.AsRef<MINMAXINFO>((void*)lParam);
                    if (MinWidth >= 0) minMaxInfo.ptMinTrackSize.x = WinUtil.ConvertToPixels(hWnd, MinWidth);
                    if (MinHeight >= 0) minMaxInfo.ptMinTrackSize.y = WinUtil.ConvertToPixels(hWnd, MinHeight);
                    break;
                case WindowMessage.WM_CLOSE:
                    if (!OnClose())
                        return IntPtr.Zero;
                    break;
                case WindowMessage.WM_DESTROY:
                    _shellIcon.Delete();
                    break;
                case WindowMessage.WM_SHOWWINDOW:
                    if (wParam == IntPtr.Zero)
                        _shellIcon.Create();
                    else
                        _shellIcon.Delete();
                    break;
                case WinNotifyIcon.NotifyIconCallbackId:
                    OnShellNotifyIconMessage((WindowMessage)(short)lParam);
                    break;
                case WindowMessage.WM_HOTKEY:
                    OnHotkey(
                        wParam.ToInt32(),
                        ((Interop.User32.Modifiers)(short)lParam).ToVirtualKeyModifiers().ToKeyModifiers(),
                        (Key)WinUtil.HiWord(lParam));
                    break;
            }

            if (msg == TaskBarCreatedMsgId)
                _shellIcon.OnTaskBarCreated();

            if (msg == RestoreMsgId)
            {
                Restore();
                SetForegroundWindow(hWnd);
            }

            return User32.CallWindowProc(_oldWndProc, hWnd, (int)msg, wParam, lParam);
        }
        catch
        {
            _shellIcon.Delete();
            throw;
        }
    }

    protected virtual void OnShellNotifyIconMessage(WindowMessage id)
    {
    }

    protected virtual void OnHotkey(int id, KeyModifiers modifiers, Key key)
    {
    }

    protected virtual bool OnClose() => true;
}