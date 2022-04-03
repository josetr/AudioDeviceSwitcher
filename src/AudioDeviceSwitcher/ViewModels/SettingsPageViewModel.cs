// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using MvvmGen;

[ViewModel]
public sealed partial class SettingsPageViewModel
{
    private readonly AudioSwitcher _audioSwitcher;
    private readonly IO _io;
    private readonly IApp app;
    [Property] private bool _runInBackground;
    [Property] private bool _runAtStartup;
    [Property] private bool _runAtStartupMinimized;
    [Property] private bool _switchCommunicationDevice;
    [Property] private bool _showDisabledDevices;
    [Property] private bool _darkTheme;

    public SettingsPageViewModel(AudioSwitcher audioSwitcher, IO io, IApp app)
    {
        _audioSwitcher = audioSwitcher;
        _io = io;
        this.app = app;
        OnInitialize();
        InitializeCommands();
    }

    public async Task LoadAsync()
    {
        await _audioSwitcher.LoadAsync();
        RunAtStartup = _audioSwitcher.RunAtStartup;
        RunInBackground = _audioSwitcher.RunInBackground;
        RunAtStartupMinimized = _audioSwitcher.RunAtStartupMinimized;
        SwitchCommunicationDevice = _audioSwitcher.SwitchCommunicationDevice;
        ShowDisabledDevices = _audioSwitcher.ShowDisabledDevices;
        DarkTheme = _audioSwitcher.DarkTheme;
    }

    [Command]
    public async Task SetRunAtStartupAsync()
    {
        if (!await _audioSwitcher.SetRunAtStartupTask(RunAtStartup))
        {
            var message = "Please go to Task Manager > Startup to enable it. Windows only allows you to enable it from the same place where you previously disabled it.";
            await _io.ShowErrorAsync(message);
        }
    }

    [Command] public void SetRunAtStartupMinimized() => _audioSwitcher.SetRunAtStartupMinimized(RunAtStartupMinimized);
    [Command] public void SetRunInBackground() => _audioSwitcher.SetRunInBackground(RunInBackground);
    [Command] public void SetSwitchCommunicationDevice() => _audioSwitcher.SetSwitchCommunicationDevice(SwitchCommunicationDevice);
    [Command] public void SetShowDisabledDevices() => _audioSwitcher.SetShowDisabledDevices(ShowDisabledDevices);

    [Command]
    public void SetDarkTheme()
    {
        _audioSwitcher.SetDarkTheme(DarkTheme);
        app.UpdateTheme();
    }
}
