// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Text.RegularExpressions;
using MvvmGen;
using Windows.Devices.Enumeration;

[ViewModel]
public sealed partial class AudioDeviceViewModel
{
    [Property] private string _id;
    [Property] private string _fullName;
    [Property] private DeviceThumbnail _img;
    [Property] private bool _isDefault;
    [Property] private bool _isDefaultCommunication;
    [Property] private bool _isEnabled;

    [PropertyInvalidate(nameof(IsEnabled))]
    public bool IsDisabled => !IsEnabled;

    [PropertyInvalidate(nameof(FullName))]
    public string Name
    {
        get
        {
            var name = GetFullNameParts().Groups[1].Value;
            return name.Length > 0 ? name : FullName;
        }
    }

    public bool IsSelected { get; set; } = true;

    [PropertyInvalidate(nameof(FullName))]
    public string DeviceName => GetFullNameParts().Groups[2].Value;

    public static AudioDeviceViewModel Create(IAudioManager audioManager, AudioDevice deviceInfo, AudioDeviceClass deviceClass)
    {
        return new AudioDeviceViewModel
        {
            FullName = deviceInfo.Name,
            Id = deviceInfo.Id,
            IsEnabled = audioManager.IsActive(deviceInfo.Id),
            IsDefault = audioManager.IsDefault(deviceInfo.Id, deviceClass, AudioDeviceRoleType.Default),
            IsDefaultCommunication = audioManager.IsDefault(deviceInfo.Id, deviceClass, AudioDeviceRoleType.Communications),
        };
    }

    public void Update(AudioDeviceChanges changes)
    {
        if (changes.FullName != null)
            FullName = changes.FullName;

        if (changes.IsEnabled.HasValue)
            IsEnabled = changes.IsEnabled.Value;
    }

    private Match GetFullNameParts()
    {
        return Regex.Match(FullName, @"(.+) \((.+)\)");
    }
}
