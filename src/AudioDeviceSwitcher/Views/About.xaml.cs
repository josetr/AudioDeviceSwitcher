// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        Version = Process.GetCurrentProcess().MainModule?.FileVersionInfo?.ProductVersion?.ToString();
        InitializeComponent();
    }

    public string? Version { get; }
}
