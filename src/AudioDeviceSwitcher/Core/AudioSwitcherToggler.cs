// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher.Core.Application;

public sealed class AudioSwitcherToggler
{
    private readonly IAudioManager audioManager;
    private readonly INotificationService notificationService;

    public AudioSwitcherToggler(IAudioManager audioManager, INotificationService notificationService)
    {
        this.audioManager = audioManager;
        this.notificationService = notificationService;
    }

    public async Task ToggleAsync(AudioDeviceClass deviceClass, IEnumerable<string> devices, bool switchCommunicationDevice)
    {
        var availableDevices = await audioManager.GetAllDevicesAsync(deviceClass);

        if (!devices.Any())
            throw new AudioSwitcherException("Please select one or more devices.");

        var skipped = new List<string>();
        var defaultAudioDevice = audioManager.GetDefaultAudioId(deviceClass, AudioDeviceRoleType.Default);

        foreach (var deviceId in GetNext(devices.ToArray(), defaultAudioDevice))
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                continue;

            var device = availableDevices.FirstOrDefault(x => x.Id == deviceId);
            var deviceName = device?.Name ?? deviceId;

            if (!audioManager.GetState(deviceId).HasFlag(AudioDeviceState.Active))
            {
                skipped.Add($"⚠️ Skipped '{deviceName}' because it is {audioManager.GetState(deviceId).ToString().ToLower()}.");
                continue;
            }

            if (!audioManager.SetDefaultDevice(deviceId, AudioDeviceRoleType.Default))
            {
                skipped.Add($"⚠️ Skipped '{deviceName}' because it may not exist.");
                continue;
            }

            if (audioManager.GetDefaultAudioId(deviceClass, AudioDeviceRoleType.Default) != deviceId)
            {
                skipped.Add($"⚠️ Skipped '{deviceName}' because it may be disconnected.");
                continue;
            }

            if (switchCommunicationDevice)
                audioManager.SetDefaultDevice(deviceId, AudioDeviceRoleType.Communications);

            if (deviceId != defaultAudioDevice || skipped.Count <= 0)
            {
                await notificationService.ShowNotificationAsync($"✔️ {deviceName}");
                return;
            }

            break;
        }

        if (skipped.Count > 0)
            await notificationService.ShowNotificationAsync(string.Join(Environment.NewLine, skipped));
    }

    private static IEnumerable<string> GetNext(string[] devices, string currentDeviceId)
    {
        for (int i = 0; i < devices.Length; ++i)
        {
            var device = devices[i];
            if (device == currentDeviceId)
                return devices.TakeLast(devices.Length - i - 1).Concat(devices.Take(i));
        }

        return devices;
    }
}
