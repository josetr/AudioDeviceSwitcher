// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Text.RegularExpressions;
using AudioDeviceSwitcher.Interop;

public class ComAudioManager : IAudioManager
{
    public virtual Task<AudioDevice[]> GetAllDevicesAsync(AudioDeviceClass type)
    {
        try
        {
            var nameKey = MMDeviceConstants.KEY_Device_FriendlyName;
            var flags = MMDeviceConstants.DEVICE_STATE_ACTIVE | MMDeviceConstants.DEVICE_STATE_UNPLUGGED | MMDeviceConstants.DEVICE_STATE_DISABLED;
            var result = new List<AudioDevice>();
            using var deviceEnumerator = new ComObject<IMMDeviceEnumerator, MMDeviceEnumerator>();
            ComUtil.CheckResult(deviceEnumerator.Value.EnumAudioEndpoints(type.ToComDataFlow(), flags, out var deviceList));
            ComUtil.CheckResult(deviceList.GetCount(out var deviceCount));

            for (uint i = 0; i < deviceCount; ++i)
            {
                try
                {
                    ComUtil.CheckResult(deviceList.Item(i, out var device));
                    ComUtil.CheckResult(device.GetId(out var id));
                    ComUtil.CheckResult(device.OpenPropertyStore(StorageConstants.STGM_READ, out var props));
                    ComUtil.CheckResult(props.GetValue(ref nameKey, out var variant));
                    var name = variant.GetValueOrDefault(string.Empty);
                    PropVariant.Clear(ref variant);
                    ComUtil.CheckResult(device.GetState(out var state));
                    result.Add(new AudioDevice(id, name));
                }
                catch
                {
                    continue;
                }
            }

            return Task.FromResult(result.ToArray());
        }
        catch
        {
            return Task.FromResult(Array.Empty<AudioDevice>());
        }
    }

    public virtual string GetDefaultAudioId(AudioDeviceClass type, AudioDeviceRoleType role)
    {
        try
        {
            using var deviceEnumerator = new ComObject<IMMDeviceEnumerator, MMDeviceEnumerator>();
            ComUtil.CheckResult(deviceEnumerator.Value.GetDefaultAudioEndpoint(type.ToComDataFlow(), role.ToComRole(), out var device));
            ComUtil.CheckResult(device.GetId(out var id));
            return id;
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool SetDefaultDevice(string id, AudioDeviceRoleType role)
    {
        try
        {
            using var policyConfig = new ComObject<IPolicyConfig, PolicyConfig>();
            return policyConfig.Value.SetDefaultEndpoint(GetAudioId(id), role.ToComRole()) == 0;
        }
        catch
        {
            return false;
        }
    }

    public void SetVisibility(string id, bool visible)
    {
        using var policyConfig = new ComObject<IPolicyConfig, PolicyConfig>();
        ComUtil.CheckResult(policyConfig.Value.SetEndpointVisibility(GetAudioId(id + 1), visible ? 1 : 0));
    }

    public bool IsDefault(string id, AudioDeviceClass type, AudioDeviceRoleType role)
    {
        return GetDefaultAudioId(type, role) == id;
    }

    public AudioDeviceState GetState(string id)
    {
        try
        {
            using var enumeratorOwner = new ComObject<IMMDeviceEnumerator, MMDeviceEnumerator>();
            ComUtil.CheckResult(enumeratorOwner.Value.GetDevice(GetAudioId(id), out var device));
            using var deviceOwner = new ComObject<IMMDevice>(device);
            ComUtil.CheckResult(device.GetState(out var state));
            return state.ToState();
        }
        catch
        {
            return AudioDeviceState.Invalid;
        }
    }

    private static string GetAudioId(string id) => Regex.Match(id, @"{.*}\.{[a-zA-Z0-9-]*}").Value;
}
