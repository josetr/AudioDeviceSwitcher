// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher.Interop;

using System.Runtime.InteropServices;

public sealed class Ole32
{
    [DllImport("ole32.dll")]
    internal static extern int PropVariantClear(ref PropVariant pvar);
}
