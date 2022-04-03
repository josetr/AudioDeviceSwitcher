// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public sealed class ComUtil
{
    public static void CheckResult(int result)
    {
        if (result < 0)
            throw new AudioSwitcherException($"COM function failed with error {result}");
    }
}
