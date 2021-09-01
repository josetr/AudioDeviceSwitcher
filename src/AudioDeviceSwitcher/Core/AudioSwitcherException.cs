// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using System;

    public class AudioSwitcherException : Exception
    {
        public AudioSwitcherException(string? message)
            : base(message)
        {
        }
    }
}