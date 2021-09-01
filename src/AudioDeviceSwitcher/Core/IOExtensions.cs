﻿// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using System.Threading.Tasks;

    public static class IOExtensions
    {
        public static Task ShowErrorAsync(this IO io, string message)
            => io.ShowMessageAsync("Error", message);
    }
}