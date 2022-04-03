// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

public sealed class AudioDeviceWatcher : IDisposable
{
    private readonly SemaphoreSlim enumerationCompletedSignal = new(0, 1);
    private readonly AudioEvents events;
    private AudioDeviceClass deviceClass;
    private DeviceWatcher? deviceWatcher = null;
    private bool started = false;

    public AudioDeviceWatcher(AudioEvents events)
    {
        this.events = events;
    }

    public AudioDeviceClass DeviceClass => deviceClass;

    public async Task StartAsync(AudioDeviceClass deviceClass)
    {
        if (started)
            return;

        this.deviceClass = deviceClass;
        started = true;

        if (deviceClass == AudioDeviceClass.Render)
            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
        else if (deviceClass == AudioDeviceClass.Capture)
            MediaDevice.DefaultAudioCaptureDeviceChanged += MediaDevice_DefaultAudioCaptureDeviceChanged;

        if (deviceWatcher == null)
        {
            deviceWatcher = DeviceInformation.CreateWatcher(AudioManager.GetInterfaceGuid(deviceClass));
            deviceWatcher.EnumerationCompleted += Watcher_EnumerationCompleted;
            deviceWatcher.Added += Watcher_Added;
            deviceWatcher.Removed += Watcher_Removed;
            deviceWatcher.Updated += Watcher_Updated;
        }

        deviceWatcher.Start();
        await enumerationCompletedSignal.WaitAsync();
    }

    public void Stop()
    {
        if (!started)
            return;

        started = false;
        deviceWatcher?.Stop();

        if (deviceClass == AudioDeviceClass.Render)
            MediaDevice.DefaultAudioRenderDeviceChanged -= MediaDevice_DefaultAudioRenderDeviceChanged;
        else if (deviceClass == AudioDeviceClass.Capture)
            MediaDevice.DefaultAudioCaptureDeviceChanged -= MediaDevice_DefaultAudioCaptureDeviceChanged;
    }

    public void Dispose()
    {
        Stop();
        deviceWatcher = null;
    }

    private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
    {
        events.Raise(new DefaultAudioDeviceChanged(args.Role.ToRole(), args.Id));
    }

    private void MediaDevice_DefaultAudioCaptureDeviceChanged(object sender, DefaultAudioCaptureDeviceChangedEventArgs args)
    {
        events.Raise(new DefaultAudioDeviceChanged(args.Role.ToRole(), args.Id));
    }

    private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
    {
        events.Raise(new AudioDeviceAdded(args.Id, new AudioDevice(args.Id, args.Name)));
    }

    private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        string? fullName = null;
        bool? isEnabled = null;

        foreach (var property in args.Properties)
        {
            if (property.Key == "System.ItemNameDisplay")
                fullName = (string)property.Value;
            else if (property.Key == "System.Devices.InterfaceEnabled")
                isEnabled = (bool)property.Value;
        }

        events.Raise(new AudioDeviceUpdated(args.Id, new(isEnabled, fullName)));
    }

    private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        events.Raise(new AudioDeviceRemoved(args.Id));
    }

    private void Watcher_EnumerationCompleted(DeviceWatcher sender, object args)
    {
        enumerationCompletedSignal.Release();
    }
}
