// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using System;
    using Windows.Devices.Enumeration;

    public sealed class Command
    {
        public Command(string name, DeviceClass type = default)
        {
            Name = name;
            DeviceClass = type;
        }

        public Command()
        {
        }

        public string Name { get; set; } = string.Empty;
        public CommandType Action { get; set; }
        public DeviceClass DeviceClass { get; set; }
        public Hotkey Hotkey { get; set; } = new();
        public string[] Devices { get; set; } = Array.Empty<string>();
    }
}