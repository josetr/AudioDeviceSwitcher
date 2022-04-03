// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public static class StringExtensions
{
    public static uint GetDjb2HashCode(this string str)
    {
        unchecked
        {
            uint hash = 5381;

            for (int i = 0; i < str.Length; i += 2)
                hash = ((hash << 5) + hash) ^ str[i];

            return hash;
        }
    }
}
