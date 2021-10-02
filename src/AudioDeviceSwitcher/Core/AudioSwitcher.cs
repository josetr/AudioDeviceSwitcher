// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AudioDeviceSwitcher.Interop;
    using Windows.Devices.Enumeration;

    public class AudioSwitcher
    {
        public AudioSwitcher(Settings? settings = null)
        {
            Settings = settings ?? new();
        }

        public List<Command> Commands { get; set; } = new();
        public Settings Settings { get; set; }
        public IntPtr Hwnd { get; set; }
        public IO? IO { get; internal set; }

        public Command AddCommand(string name, DeviceClass deviceClass)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new AudioSwitcherException("Name cannot be empty");

            if (Commands.Any(x => x.Name.ToLower() == name.ToLower()))
                throw new AudioSwitcherException($"Name '{name}' is already in use");

            var command = new Command(name, deviceClass);
            Commands.Add(command);
            SaveSettings();
            return command;
        }

        public void RenameCommand(string name, string newName)
        {
            newName = newName.Trim();

            var command = Commands.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
            if (command == null)
                throw new AudioSwitcherException($"Command '{name}' doesn't exist.");

            var newCommand = Commands.FirstOrDefault(x => x.Name.ToLower() == newName.ToLower());
            if (newCommand != null)
                throw new AudioSwitcherException($"Name '{newName}' is already in use.");

            UnregisterCommandHotkey(command);
            command.Name = newName;
            SaveSettings();
        }

        public void DeleteCommand(string name)
        {
            var command = Commands.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
            if (command == null)
                throw new AudioSwitcherException("Command doesn't exist.");

            if (Commands.Count(x => x.DeviceClass == command.DeviceClass) <= 1)
                throw new AudioSwitcherException("It's not possible to delete the last command.");

            UnregisterCommandHotkey(command);
            Commands.Remove(command);
            SaveSettings();
        }

        public void Load()
        {
            Settings = Settings.Load();
            Commands = Settings.Commands.ToList();

            if (!Commands.Any(x => x.DeviceClass == DeviceClass.AudioRender))
                Commands.Add(new("Default command", DeviceClass.AudioRender));

            if (!Commands.Any(x => x.DeviceClass == DeviceClass.AudioCapture))
                Commands.Add(new("Default recording command", DeviceClass.AudioCapture));
        }

        public void SaveSettings()
        {
            Settings.Commands = Commands.ToArray();
            Settings.Save();
        }

        public void RegisterHotkeys()
        {
            Commands.ForEach(command => UnregisterCommandHotkey(command));
            Commands.ForEach(command => RegisterCommandHotkey(command));
        }

        public void RegisterCommandHotkey(Command command)
        {
            if (command.Hotkey != Hotkey.Empty)
                User32.RegisterHotKey(Hwnd, command.Name.GetHashCode(), User32.ToModifiers(command.Hotkey.Modifiers), command.Hotkey.Key);
        }

        public void UnregisterCommandHotkey(Command command)
        {
            while (User32.UnregisterHotKey(Hwnd, command.Name.GetHashCode()))
            {
            }
        }

        public static string GetCommandArgs(DeviceClass deviceClass, Device[] devices)
        {
            var type = deviceClass switch
            {
                DeviceClass.AudioCapture => "recording",
                DeviceClass.AudioRender => "playback",
                _ => throw new NotImplementedException(),
            };

            if (devices.Count() >= 1)
                return $"{string.Join(" ", devices.Select(x => QN(x)))} -{type}";
            else
                return string.Empty;

            static string QN(Device name)
            {
                return $"\"{name.Name} / {(uint)name.Id.GetHashCode()}\"";
            }
        }

        public async Task OnHotkeyAsync(Hotkey hotkey)
        {
            var command = Commands.FirstOrDefault(x => x.Hotkey == hotkey);
            if (command == null)
                throw new AudioSwitcherException($"Command with hotkey '{hotkey}' doesn't exist.");

            await ToggleAsync(command.DeviceClass, command.Devices, Settings.SwitchCommunicationDevice);
        }

        public async Task ToggleAsync(DeviceClass deviceClass, IEnumerable<string> devices, bool com, IEnumerable<DeviceInformation>? availableDevices = null)
        {
            if (availableDevices == null)
                availableDevices = await DeviceInformation.FindAllAsync(AudioUtil.GetInterfaceGuid(deviceClass));

            if (!devices.Any())
                throw new AudioSwitcherException("Please select one or more devices.");

            var skipped = new StringBuilder();
            var defaultAudioDevice = AudioUtil.GetDefaultAudioId(deviceClass);

            foreach (var deviceId in GetNext(devices.ToArray(), defaultAudioDevice))
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                    continue;

                var device = availableDevices.FirstOrDefault(x => x.Id == deviceId);
                var deviceName = device?.Name ?? deviceId;

                if (!AudioUtil.SetDefaultDevice(deviceId))
                {
                    skipped.AppendLine($"⚠️ Skipped '{deviceName} because it may not exist.");
                    continue;
                }

                if (AudioUtil.GetDefaultAudioId(deviceClass) != deviceId)
                {
                    skipped.AppendLine($"⚠️ Skipped '{deviceName}' because it may be disconnected.");
                    continue;
                }

                if (com)
                    AudioUtil.SetDefaultDevice(deviceId, ERole.eCommunications);

                if (deviceId == defaultAudioDevice && skipped.Length > 0)
                    IO?.ShowNotification(skipped.ToString());
                else
                    IO?.ShowNotification($"✔️ {deviceName}");

                return;
            }
        }

        public static IEnumerable<string> GetNext(string[] devices, string currentDeviceId)
        {
            for (int i = 0; i < devices.Length; ++i)
            {
                var device = devices[i];
                if (device == currentDeviceId)
                    return devices.TakeLast(devices.Length - i - 1).Concat(devices.Take(i + 1));
            }

            return devices;
        }
    }
}