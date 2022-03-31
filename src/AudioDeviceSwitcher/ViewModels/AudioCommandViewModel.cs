// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using MvvmGen;

[ViewModel]
public partial class AudioCommandViewModel
{
    [Property] private string _name = string.Empty;
    [Property] private Hotkey _hotkey = new();
    [Property] private string[] _deviceIds = Array.Empty<string>();
}
