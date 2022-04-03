// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AudioDeviceSwitcher.Core.Application;

public sealed partial class AudioSwitcher
{
    public const string StartupTaskId = "Startup";
    private readonly IStateStorage stateStorage;
    private readonly IStartupTaskManager startupTaskManager;
    private readonly IHotkeyManager hotkeyManager;
    private readonly INotificationService notificationService;
    private readonly IO io;
    private AudioSwitcherState state;

    public AudioSwitcher(
        IAudioManager audioManager,
        IStartupTaskManager startupTaskManager,
        IO io,
        IStateStorage stateStorage,
        IHotkeyManager hotkeyManager,
        INotificationService notificationService,
        AudioSwitcherState? initialState = null)
    {
        this.stateStorage = stateStorage;
        AudioManager = audioManager;
        state = initialState ?? new();
        this.startupTaskManager = startupTaskManager;
        this.hotkeyManager = hotkeyManager;
        this.notificationService = notificationService;
        this.io = io;
    }

    public IAudioManager AudioManager { get; }
    public IReadOnlyList<Command> Commands => state.Commands;
    public bool RunAtStartup => state.RunAtStartup;
    public bool RunAtStartupMinimized => state.RunAtStartupMinimized;
    public bool RunInBackground => state.RunInBackground;
    public bool ShowDisabledDevices => state.ShowDisabledDevices;
    public bool SwitchCommunicationDevice => state.SwitchCommunicationDevice;
    public bool DarkTheme => state.DarkTheme;

    public async Task LoadAsync()
    {
        state = stateStorage.Load();
        var task = await startupTaskManager.GetTaskAsync(StartupTaskId);
        if (task != null)
            state.RunAtStartup = await task.IsEnabledAsync();

        if (!Commands.Any(x => x.DeviceClass == AudioDeviceClass.Render))
            state.Commands.Add(new("Default command", AudioDeviceClass.Render));

        if (!Commands.Any(x => x.DeviceClass == AudioDeviceClass.Capture))
            state.Commands.Add(new("Default recording command", AudioDeviceClass.Capture));

        RegisterHotkeys();
    }

    public void Save()
    {
        stateStorage.Save(state);
    }

    public Command AddCommand(CommandName name, AudioDeviceClass deviceClass, Hotkey? hotkey = null, string[]? devices = null)
    {
        EnsureNameIsAvailable(name);
        var command = new Command(name, deviceClass, hotkey, devices);
        state.Commands.Add(command);
        Save();
        return command;
    }

    public void RenameCommand(CommandName name, CommandName newName)
    {
        EnsureNameIsAvailable(newName);
        var command = GetCommand(name);
        SaveCommand(command with { Name = newName }, name);
    }

    public void DeleteCommand(CommandName name)
    {
        var command = GetCommand(name);

        if (Commands.Count(x => x.DeviceClass == command.DeviceClass) <= 1)
            throw new AudioSwitcherException("Command list cannot be left empty.");

        hotkeyManager.UnregisterHotkey(command.HotkeyId);
        state.Commands.Remove(command);
        Save();
    }

    public void ChangeCommandHotkey(Command command, Hotkey hotkey)
    {
        if (hotkey != Hotkey.Empty)
        {
            var cmd = Commands.FirstOrDefault(x => x.Hotkey == hotkey && x.DeviceClass == command.DeviceClass);
            if (cmd != null && cmd.Name != command.Name)
                throw new AudioSwitcherException($"'{hotkey}' is already in use by command '{cmd.Name}'");
        }

        SaveCommand(command with { Hotkey = hotkey });
    }

    public Task ExecuteCommandByHotkeyAsync(Hotkey hotkey)
    {
        var commands = Commands.Where(x => x.Hotkey == hotkey).OrderBy(x => x.DeviceClass);
        if (!commands.Any())
            throw new AudioSwitcherException($"Command with hotkey '{hotkey}' doesn't exist.");

        return ExecuteCommandsAsync(commands);
    }

    public Task ExecuteCommandAsync(Command command)
    {
        return ExecuteCommandsAsync(new[] { command });
    }

    public async Task ExecuteCommandsAsync(IEnumerable<Command> commands)
    {
        var notificationService = new GroupNotificationService(this.notificationService);
        foreach (var command in commands)
            await ToggleAsync(command.DeviceClass, command.Devices, notificationService);
        notificationService.Commit();
    }

    public void RegisterHotkeys()
    {
        var commands = Commands.Where(x => x.Hotkey != Hotkey.Empty).ToList();
        commands.ForEach(command => hotkeyManager.UnregisterHotkey(command.HotkeyId));
        commands.ForEach(command => hotkeyManager.RegisterHotkey(command.HotkeyId, command.Hotkey));
    }

    public void SaveCommand(Command command, string? name = null)
    {
        name ??= command.Name;
        var index = state.Commands.FindIndex(x => x.Name == name);
        if (index < 0)
            return;

        if (!string.Equals(Commands[index].Name, command.Name))
            hotkeyManager.UnregisterHotkey(Commands[index].HotkeyId);

        state.Commands[index] = command;
        Save();
        RegisterHotkeys();
    }

    public async Task ToggleAsync(AudioDeviceClass deviceClass, IEnumerable<string> devices, INotificationService notificationService)
    {
        var toggler = new AudioSwitcherToggler(AudioManager, notificationService);
        await toggler.ToggleAsync(deviceClass, devices, state.SwitchCommunicationDevice);
    }

    public async Task<bool> SetRunAtStartupTask(bool enable)
    {
        var startupTask = await startupTaskManager.GetTaskAsync(StartupTaskId);

        if (!enable)
            await startupTask.DisableAsync();
        else
            await startupTask.EnableAsync();

        var isEnabled = await startupTask.IsEnabledAsync();
        state.RunAtStartup = isEnabled;

        if (enable && !isEnabled)
            return false;

        Save();
        return true;
    }

    public void SetRunAtStartupMinimized(bool runAtStartupMinimized)
    {
        state.RunAtStartupMinimized = runAtStartupMinimized;
        Save();
    }

    public void SetRunInBackground(bool runInBackground)
    {
        state.RunInBackground = runInBackground;
        Save();
    }

    public void SetSwitchCommunicationDevice(bool switchCommunicationDevice)
    {
        state.SwitchCommunicationDevice = switchCommunicationDevice;
        Save();
    }

    public void SetShowDisabledDevices(bool showDisabledDevices)
    {
        state.ShowDisabledDevices = showDisabledDevices;
        Save();
    }

    public void SetDarkTheme(bool darkTheme)
    {
        state.DarkTheme = darkTheme;
        Save();
    }

    private bool TryGetCommand(string name, [MaybeNullWhen(false)] out Command command)
    {
        command = Commands.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        return command != null;
    }

    private void EnsureNameIsAvailable(string name)
    {
        if (TryGetCommand(name, out var _))
            throw new AudioSwitcherException($"Name '{name}' is already in use.");
    }

    private Command GetCommand(string name)
    {
        if (!TryGetCommand(name, out var command))
            throw new AudioSwitcherException($"Command '{name}' doesn't exist.");

        return command;
    }
}