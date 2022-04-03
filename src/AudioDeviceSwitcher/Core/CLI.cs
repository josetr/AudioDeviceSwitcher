// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using AudioDeviceSwitcher.Core.Application;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public sealed class CLI
{
    private readonly IAudioManager audioManager;
    private readonly IStateStorage stateStorage;
    private readonly INotificationService notificationService;

    public CLI(IAudioManager audioManager, IStateStorage stateStorage, INotificationService notificationService)
    {
        this.stateStorage = stateStorage;
        this.audioManager = audioManager;
        this.notificationService = notificationService;
    }

    public async Task RunAsync(string cmd)
    {
        await RunAsync(ParseArguments(cmd));
    }

    public async Task RunAsync(string[] cmdArgs)
    {
        ParseArguments(cmdArgs, out var device, out var type);

        if (!device.Any())
            throw new CLIException("Missing device name.");

        var availableDevices = await audioManager.GetAllDevicesAsync(type);
        var uniqueDeviceIds = new HashSet<string>(device.Select(x => GetDeviceId(x, availableDevices)));
        var state = stateStorage.Load();
        var toggler = new AudioSwitcherToggler(audioManager, notificationService);
        await toggler.ToggleAsync(type, uniqueDeviceIds, state.SwitchCommunicationDevice);
    }

    public static string GetDeviceId(string devicePart, AudioDevice[] deviceList)
    {
        var (name, uid) = DecodeName(devicePart);
        var found = false;
        var devices = deviceList.Where(x => x.Name == name);

        if (devices.Count() > 1)
        {
            found = true;
            devices = devices.Where(x => x.Id.GetDjb2HashCode() == uid);
            if (devices.Count() > 1)
                throw CreateError("Found two devices with the same name.");
        }

        if (devices.Count() == 1)
            return devices.First().Id;

        devices = deviceList.Where(x => x.Id.GetDjb2HashCode() == uid);
        if (devices.Count() > 1)
        {
            found = true;
            devices = devices.Where(x => x.Name == name);
            if (devices.Count() > 1)
                throw CreateError("Found two devices with the same name.");
        }

        if (devices.Count() == 1)
            return devices.First().Id;

        if (!found)
            throw CreateError($"Device \"{name}\" doesn't exist.");

        throw CreateError();

        static CLIException CreateError(string? msg = null)
        {
            return new($"{(msg != null ? msg + " " : string.Empty)}Please regenerate your command.");
        }
    }

    public static string BuildCommand(AudioDeviceClass deviceClass, AudioDevice[] devices)
    {
        if (!devices.Any())
            return string.Empty;

        return $"AudioDeviceSwitcher {BuildCommandArgs(deviceClass, devices)}";
    }

    public static string BuildCommandArgs(AudioDeviceClass deviceClass, AudioDevice[] devices)
    {
        if (devices.Count() >= 1)
            return $"{string.Join(" ", devices.Select(x => EncodeName(x)))} -{EncodeClass(deviceClass)}";

        return string.Empty;
    }

    public static string EncodeName(AudioDevice name)
    {
        return $"\"{name.Name} / {name.Id.GetDjb2HashCode()}\"";
    }

    public static (string Name, uint Uid) DecodeName(string text)
    {
        var match = Regex.Match(text, @"^(.+?)(?: / ?(\d+))?$");
        if (!match.Success)
            throw new CLIException("Invalid device name. Please regenerate your command.");

        string name = match.Groups[1].Value;
        uint uid = 0;
        if (match.Groups.Count > 2)
            uint.TryParse(match.Groups[2].Value, out uid);
        return (name, uid);
    }

    public static string EncodeClass(AudioDeviceClass deviceClass)
    {
        return deviceClass switch
        {
            AudioDeviceClass.Capture => "recording",
            AudioDeviceClass.Render => "playback",
            _ => throw new NotImplementedException(),
        };
    }

    public static bool DecodeClass(string text, out AudioDeviceClass? deviceClass)
    {
        switch (text)
        {
            case "-recording":
                deviceClass = AudioDeviceClass.Capture;
                return true;
            case "-playback":
                deviceClass = AudioDeviceClass.Render;
                return true;
            default:
                deviceClass = default;
                return false;
        }
    }

    public static void ParseArguments(string[] cmdArgs, out string[] devices, out AudioDeviceClass type)
    {
        AudioDeviceClass? localType = null;

        devices = cmdArgs.Where(arg =>
        {
            if (DecodeClass(arg, out localType))
                return false;

            return true;
        }).ToArray();

        type = localType.GetValueOrDefault(AudioDeviceClass.Render);
    }

    public static string[] ParseArguments(string cmd)
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
