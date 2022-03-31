// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using MvvmGen;

[ViewModel]
public sealed partial class SettingEntryViewModel
{
    [Property] private bool _isSelected;
    [Property] private string _name;
    [Property] private string _reason;
    [Property] private Func<bool, Task> _handler;
}
