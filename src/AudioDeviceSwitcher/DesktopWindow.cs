// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using AudioDeviceSwitcher.Interop;
    using Microsoft.UI.Xaml;
    using Windows.System;
    using WinRT.Interop;
    using static PInvoke.User32;

    public class DesktopWindow : Window
    {
        private const WindowMessage NotifyIconCallbackId = WindowMessage.WM_USER + 1000;
        private static WindowMessage taskBarCreatedMsgId = (WindowMessage)RegisterWindowMessage("TaskbarCreated");
        private static WindowMessage restoreMsgId = (WindowMessage)RegisterWindowMessage(App.Id);

        private static Shell32.NOTIFYICONDATA notifyIconData;
        private IntPtr _hwnd;
        private IntPtr _oldWndProc;
        private WinProc? _newWndProc;
        private bool notifyIconCreated;

        public DesktopWindow()
        {
            SubClass();
        }

        public delegate void HotkeyDelegate(int id, VirtualKeyModifiers mod, Windows.System.VirtualKey ukey);
        private delegate IntPtr WinProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

        public IntPtr Hwnd => _hwnd;
        public int MinWidth { get; set; } = -1;
        public int MinHeight { get; set; } = -1;
        public IntPtr IconHandle { get; set; }
        public bool IsVisible => IsWindowVisible(_hwnd);
        public bool IsActive => GetActiveWindow() == _hwnd;

        public void Maximize() => _ = ShowWindow(_hwnd, WindowShowStyle.SW_MAXIMIZE);
        public void Minimize() => _ = ShowWindow(_hwnd, WindowShowStyle.SW_MINIMIZE);
        public void Restore() => _ = ShowWindow(_hwnd, WindowShowStyle.SW_RESTORE);
        public void Hide() => _ = ShowWindow(_hwnd, WindowShowStyle.SW_HIDE);

        public void SetWindowSize(int width, int height)
        {
            SetWindowPos(Hwnd, SpecialWindowHandles.HWND_TOP, 0, 0, width, height, SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOACTIVATE);
        }

        public void SetIcon(string name)
        {
            const int ICON_SMALL = 0;
            const int ICON_BIG = 1;
            SendMessage(Hwnd, WindowMessage.WM_SETICON, (IntPtr)ICON_SMALL, IconHandle = LoadIcon(name, big: false));
            SendMessage(Hwnd, WindowMessage.WM_SETICON, (IntPtr)ICON_BIG, LoadIcon(name, big: true));
        }

        public static void BroadcastRestore()
        {
            PostMessage(HWND_BROADCAST, restoreMsgId, IntPtr.Zero, IntPtr.Zero);
        }

        protected virtual unsafe IntPtr NewWindowProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                switch (msg)
                {
                    case WindowMessage.WM_GETMINMAXINFO:
                        ref var minMaxInfo = ref Unsafe.AsRef<MINMAXINFO>((void*)lParam);
                        if (MinWidth >= 0) minMaxInfo.ptMinTrackSize.x = ConvertToPixels(MinWidth);
                        if (MinHeight >= 0) minMaxInfo.ptMinTrackSize.y = ConvertToPixels(MinHeight);
                        break;
                    case WindowMessage.WM_CLOSE:
                        if (!OnClose())
                            return IntPtr.Zero;
                        break;
                    case WindowMessage.WM_DESTROY:
                        DeleteNotifyIcon();
                        break;
                    case WindowMessage.WM_SHOWWINDOW:
                        {
                            if (wParam == IntPtr.Zero)
                                CreateNotifyIcon();
                            else
                                DeleteNotifyIcon();
                        }

                        break;
                    case NotifyIconCallbackId:
                        OnShellNotifyIconMessage((WindowMessage)(short)lParam);
                        break;
                    case WindowMessage.WM_HOTKEY:
                        OnHotkey(
                            wParam.ToInt32(),
                            User32.ToVirtualKeyModifiers((User32.Modifiers)(short)lParam),
                            (Windows.System.VirtualKey)HiWord(lParam));
                        break;
                }

                if (msg == taskBarCreatedMsgId)
                    OnTaskBarCreated();

                if (msg == restoreMsgId)
                {
                    Restore();
                    SetForegroundWindow(hWnd);
                }

                return User32.CallWindowProc(_oldWndProc, hWnd, (int)msg, wParam, lParam);
            }
            catch
            {
                DeleteNotifyIcon();
                throw;
            }
        }

        protected virtual void OnShellNotifyIconMessage(WindowMessage id)
        {
        }

        protected virtual void OnHotkey(int id, VirtualKeyModifiers modifiers, Windows.System.VirtualKey key)
        {
        }

        protected virtual bool OnClose()
        {
            return true;
        }

        protected virtual void OnTaskBarCreated()
        {
            notifyIconCreated = false;
            CreateNotifyIcon();
        }

        protected static IntPtr LoadIcon(string iconName, bool big = true)
        {
            return LoadImage(
                IntPtr.Zero,
                iconName,
                ImageType.IMAGE_ICON,
                GetSystemMetrics(big ? SystemMetric.SM_CXICON : SystemMetric.SM_CXSMICON),
                GetSystemMetrics(big ? SystemMetric.SM_CYICON : SystemMetric.SM_CYSMICON),
                LoadImageFlags.LR_LOADFROMFILE);
        }

        protected void CreateNotifyIcon()
        {
            if (notifyIconCreated)
                return;

            notifyIconCreated = true;
            notifyIconData.hWnd = Hwnd;
            notifyIconData.uID = 10000;
            notifyIconData.szTip = Title;
            notifyIconData.hIcon = IconHandle;
            notifyIconData.cbSize = Marshal.SizeOf(notifyIconData);
            notifyIconData.uFlags = Shell32.NIF_ICON | Shell32.NIF_MESSAGE | Shell32.NIF_TIP;
            notifyIconData.uCallbackMessage = (int)NotifyIconCallbackId;
            Shell32.Shell_NotifyIcon(Shell32.NIM_ADD, in notifyIconData);
        }

        protected void DeleteNotifyIcon()
        {
            if (!notifyIconCreated)
                return;

            Shell32.Shell_NotifyIcon(Shell32.NIM_DELETE, in notifyIconData);
            notifyIconCreated = false;
        }

        private unsafe void SubClass()
        {
            _hwnd = WindowNative.GetWindowHandle(this);
            _newWndProc = new WinProc(NewWindowProc);
            _oldWndProc = SetWindowLongPtr(_hwnd, WindowLongIndexFlags.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
        }

        private static uint HiWord(IntPtr ptr)
        {
            uint value = (uint)(int)ptr;
            if ((value & 0x80000000) == 0x80000000)
                return value >> 16;
            else
                return (value >> 16) & 0xffff;
        }

        private int ConvertToPixels(int value)
        {
            var scalingFactor = (float)GetDpiForWindow(Hwnd) / 96;
            return (int)(value * scalingFactor);
        }
    }
}
