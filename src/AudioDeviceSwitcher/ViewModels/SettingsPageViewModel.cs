// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1201 // Elements should appear in the correct order

namespace AudioDeviceSwitcher;

using Microsoft.UI.Xaml;
using MvvmGen;
using Windows.ApplicationModel;

[ViewModel]
public partial class SettingsPageViewModel
{
    private const string StartupTaskId = "Startup";
    private readonly AudioSwitcher audioSwitcher;
    [Property] private bool _runInBackground;
    [Property] private bool _runAtStartup;
    [Property] private bool _runAtStartupMinimized;
    [Property] private bool _switchCommunicationDevice;
    [Property] private bool _showDisabledDevices;
    [Property] private bool _darkTheme;

    private Settings Settings => audioSwitcher.Settings;

    public SettingsPageViewModel(AudioSwitcher audioSwitcher)
    {
        this.audioSwitcher = audioSwitcher;
        OnInitialize();
        InitializeCommands();
    }

    public IO IO { get; set; } = new NullIO();

    public async Task Load()
    {
        audioSwitcher.Load();
        RunAtStartup = Settings.RunAtStartup = await IsStartupTaskEnabledAsync();
        RunInBackground = Settings.RunInBackground;
        RunAtStartupMinimized = Settings.RunAtStartupMinimized;
        SwitchCommunicationDevice = Settings.SwitchCommunicationDevice;
        ShowDisabledDevices = Settings.ShowDisabledDevices;
        DarkTheme = Settings.DarkTheme;
    }

    [Command]
    public async Task SetRunAtStartupAsync()
    {
        var startupTask = await StartupTask.GetAsync(StartupTaskId);
        var run = RunAtStartup;

        if (await IsStartupTaskEnabledAsync(startupTask))
            startupTask.Disable();
        else
            await startupTask.RequestEnableAsync();

        Settings.RunAtStartup = RunAtStartup = await IsStartupTaskEnabledAsync();

        if (run && !RunAtStartup)
            await IO.ShowErrorAsync("Please go to Task Manager > Startup to enable it. Windows only allows you to enable it from the same place where you previously disabled it.");
        else
            Settings.Save();
    }

    [Command]
    public void SetRunAtStartupMinimizedAsync()
    {
        Settings.RunAtStartupMinimized = RunAtStartupMinimized;
        Settings.Save();
    }

    [Command]
    public void SetRunInBackgroundAsync()
    {
        Settings.RunInBackground = RunInBackground;
        Settings.Save();
    }

    [Command]
    public void SetSwitchCommunicationDeviceAsync()
    {
        Settings.SwitchCommunicationDevice = SwitchCommunicationDevice;
        Settings.Save();
    }

    [Command]
    public void SetShowDisabledDevicesAsync()
    {
        Settings.ShowDisabledDevices = ShowDisabledDevices;
        Settings.Save();
    }

    [Command]
    public void SetDarkTheme()
    {
        if (App.Window?.Content is not FrameworkElement e)
            return;

        e.RequestedTheme = DarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        Settings.DarkTheme = DarkTheme;
        Settings.Save();
    }

    public async Task<bool> IsStartupTaskEnabledAsync(StartupTask? task = null)
    {
        var startupTask = task ?? await StartupTask.GetAsync(StartupTaskId);
        return startupTask.State == StartupTaskState.Enabled || startupTask.State == StartupTaskState.EnabledByPolicy;
    }
}
