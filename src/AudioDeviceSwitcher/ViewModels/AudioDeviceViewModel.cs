// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.UI.Xaml.Media.Imaging;
    using MvvmGen;
    using Windows.Devices.Enumeration;
    using Windows.Media.Devices;

    [ViewModel]
    public sealed partial class AudioDeviceViewModel
    {
        [Property] private string _id;
        [Property] private string _fullName;
        [Property] private BitmapImage _img;
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
                var name = Regex.Match(FullName, @"(.+) \((.+)\)").Groups[1].Value;
                return name.Length > 0 ? name : FullName;
            }
        }

        [PropertyInvalidate(nameof(FullName))]
        public string DeviceName => Regex.Match(FullName, @"(.+) \((.+)\)").Groups[2].Value;

        public static AudioDeviceViewModel Create(DeviceInformation deviceInfo, DeviceClass deviceClass)
        {
            return new AudioDeviceViewModel
            {
                FullName = deviceInfo.Name,
                Id = deviceInfo.Id,
                IsEnabled = deviceInfo.IsEnabled,
                IsDefault = AudioUtil.IsDefault(deviceInfo.Id, deviceClass),
                IsDefaultCommunication = AudioUtil.IsDefault(deviceInfo.Id, deviceClass, AudioDeviceRole.Communications),
            };
        }

        public async Task LoadImageAsync(DeviceInformation deviceInfo)
        {
            try
            {
                var img = new BitmapImage();
                var thumbnail = await deviceInfo.GetThumbnailAsync();
                await img.SetSourceAsync(thumbnail);
                Img = img;
            }
            catch
            {
                return;
            }
        }

        public void Update(DeviceInformationUpdate deviceInfo)
        {
            foreach (var prop in deviceInfo.Properties)
            {
                if (prop.Key == "System.ItemNameDisplay")
                    FullName = (string)prop.Value;
                else if (prop.Key == "System.Devices.InterfaceEnabled")
                    IsEnabled = (bool)prop.Value;
            }
        }

        public void UpdateDefault(AudioDeviceRole role, string id)
        {
            if (role == AudioDeviceRole.Default)
                IsDefault = id == Id;
            else if (role == AudioDeviceRole.Communications)
                IsDefaultCommunication = id == Id;
        }
    }
}
