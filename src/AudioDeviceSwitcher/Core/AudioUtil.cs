// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Text.RegularExpressions;
using AudioDeviceSwitcher.Interop;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

public sealed class AudioUtil
{
    public static string GetInterfaceGuid(DeviceClass type)
    {
        var selector = type == DeviceClass.AudioRender ? MediaDevice.GetAudioRenderSelector() : MediaDevice.GetAudioCaptureSelector();
        var interfaceGuid = Regex.Match(selector, "{.*}").Value;
        return $"System.Devices.InterfaceClassGuid:=\"{interfaceGuid}\"";
    }

    public static bool SetDefaultDevice(string id, ERole role = ERole.eConsole)
    {
        using var policyConfig = new ComObject<IPolicyConfig, PolicyConfig>();

        try
        {
            return policyConfig.Value.SetDefaultEndpoint(GetAudioId(id), role) == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void SetVisibility(string id, bool visible)
    {
        using var policyConfig = new ComObject<IPolicyConfig, PolicyConfig>();
        NoThrow(() => policyConfig.Value.SetEndpointVisibility(GetAudioId(id), visible ? 1 : 0));
    }

    public static bool IsDisabled(string id, bool def = false)
    {
        try
        {
            using var enumeratorOwner = new ComObject<IMMDeviceEnumerator, MMDeviceEnumerator>();
            enumeratorOwner.Value.GetDevice(GetAudioId(id), out var device);
            using var deviceOwner = new ComObject<IMMDevice>(device);
            if (device == null)
                return def;
            device.GetState(out var state);
            return state == MMDeviceState.Disabled;
        }
        catch (Exception)
        {
            return def;
        }
    }

    public static bool IsDefault(string id, DeviceClass type, AudioDeviceRole role = AudioDeviceRole.Default)
    {
        return GetDefaultAudioId(type, role) == id;
    }

    public static string GetDefaultAudioId(DeviceClass type, AudioDeviceRole role = AudioDeviceRole.Default)
    {
        if (type == DeviceClass.AudioRender)
            return MediaDevice.GetDefaultAudioRenderId(role);
        else if (type == DeviceClass.AudioCapture)
            return MediaDevice.GetDefaultAudioCaptureId(role);
        return string.Empty;
    }

    public static string GetAudioId(string id) => Regex.Match(id, @"{.*}\.{[a-zA-Z0-9-]*}").Value;

    private static void NoThrow(Action action)
    {
        try
        {
            action();
        }
        catch
        {
        }
    }
}
