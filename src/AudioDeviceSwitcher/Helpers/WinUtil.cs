// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using static PInvoke.User32;

internal static class WinUtil
{
    public static IntPtr LoadIcon(string iconName, bool big = true)
    {
        return LoadImage(
            IntPtr.Zero,
            iconName,
            ImageType.IMAGE_ICON,
            GetSystemMetrics(big ? SystemMetric.SM_CXICON : SystemMetric.SM_CXSMICON),
            GetSystemMetrics(big ? SystemMetric.SM_CYICON : SystemMetric.SM_CYSMICON),
            LoadImageFlags.LR_LOADFROMFILE);
    }

    public static int ConvertToPixels(IntPtr hwnd, int value)
    {
        var scalingFactor = (float)GetDpiForWindow(hwnd) / 96;
        return (int)(value * scalingFactor);
    }

    public static uint HiWord(IntPtr ptr)
    {
        uint value = (uint)(int)ptr;
        if ((value & 0x80000000) == 0x80000000)
            return value >> 16;
        else
            return (value >> 16) & 0xffff;
    }
}
