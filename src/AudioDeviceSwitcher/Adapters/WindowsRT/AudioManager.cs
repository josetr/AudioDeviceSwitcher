// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

public sealed class AudioManager : ComAudioManager
{
    public override async Task<AudioDevice[]> GetAllDevicesAsync(AudioDeviceClass type)
    {
        var devices = await DeviceInformation.FindAllAsync(GetInterfaceGuid(type));
        return devices.Select(x => new AudioDevice(x.Id, x.Name)).ToArray();
    }

    public override string GetDefaultAudioId(AudioDeviceClass type, AudioDeviceRoleType role)
    {
        return type switch
        {
            AudioDeviceClass.Render => MediaDevice.GetDefaultAudioRenderId(role.ToRole()),
            AudioDeviceClass.Capture => MediaDevice.GetDefaultAudioCaptureId(role.ToRole()),
            _ => throw new NotImplementedException(),
        };
    }

    public static string GetInterfaceGuid(AudioDeviceClass type)
    {
        var selector = type == AudioDeviceClass.Render ? MediaDevice.GetAudioRenderSelector() : MediaDevice.GetAudioCaptureSelector();
        var interfaceGuid = Regex.Match(selector, "{.*}").Value;
        return $"System.Devices.InterfaceClassGuid:=\"{interfaceGuid}\"";
    }
}
