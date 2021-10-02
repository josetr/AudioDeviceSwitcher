// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Windows.Devices.Enumeration;

    public class CLI
    {
        public static async Task RunAsync(string cmd)
        {
            await RunAsync(GetArgs(cmd));
        }

        public static async Task RunAsync(string[] cmdArgs)
        {
            var type = DeviceClass.AudioRender;
            var deviceInputs = cmdArgs.Where(x =>
            {
                if (x == "-recording")
                {
                    type = DeviceClass.AudioCapture;
                    return false;
                }

                return x != "-playback";
            }).ToArray();

            if (!deviceInputs.Any())
                throw new CLIException("Missing device name.");

            var devices = await DeviceInformation.FindAllAsync(AudioUtil.GetInterfaceGuid(type));
            var uniqueDeviceIds = new HashSet<string>();

            foreach (var deviceInput in deviceInputs)
                uniqueDeviceIds.Add(GetId(deviceInput, devices));

            var settings = Settings.Load();
            var audioSwitcher = new AudioSwitcher(settings);
            await audioSwitcher.ToggleAsync(type, uniqueDeviceIds, settings.SwitchCommunicationDevice, devices);
        }

        private static string GetId(string devicePart, DeviceInformationCollection devices)
        {
            var match = Regex.Match(devicePart, @"^(.*?)(?: / ?(\d+))?$");
            if (!match.Success)
                throw new CLIException("Invalid device name. Please regenerate your command.");

            var name = match.Groups[1].Value;
            uint uid = 0;

            if (match.Groups.Count > 2)
                uint.TryParse(match.Groups[2].Value, out uid);

            var id = (int)uid;
            var found = false;
            var matches = devices.Where(x => x.Name == name);

            if (matches.Count() > 1)
            {
                found = true;
                matches = matches.Where(x => x.Id.GetHashCode() == id);
                if (matches.Count() > 1)
                    throw new CLIException("Found two devices with the same name. Please regenerate your command.");
            }

            if (matches.Count() == 1)
                return matches.First().Id;

            matches = devices.Where(x => x.Id.GetHashCode() == id);
            if (matches.Count() > 1)
            {
                found = true;
                matches = matches.Where(x => x.Name == name);
                if (matches.Count() > 1)
                    throw new CLIException("Found two devices with the same name. Please regenerate your command.");
            }

            if (matches.Count() == 1)
                return matches.First().Id;

            if (!found)
                throw new CLIException($"Device \"{name}\" doesn't exist. Please regenerate your command.");
            else
                throw new CLIException("Please regenerate your command.");
        }

        private static string[] GetArgs(string cmd)
        {
            var args = new List<string>();
            var insideQuote = false;
            var part = new StringBuilder();
            for (int i = 0; i < cmd.Length; ++i)
            {
                var c = cmd[i];
                if (c == ' ')
                {
                    if (!insideQuote)
                    {
                        if (part.Length > 0)
                        {
                            args.Add(part.ToString());
                            part.Clear();
                            continue;
                        }
                    }
                }
                else if (c == '"')
                {
                    insideQuote = !insideQuote;
                    continue;
                }

                part.Append(c);
            }

            if (part.Length > 0)
                args.Add(part.ToString());

            return args.ToArray();
        }
    }
}
