// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.InteropServices.WindowsRuntime;
using MvvmGen;
using Windows.Devices.Enumeration;

[ViewModel(GenerateConstructor = false)]
public sealed partial class AudioPageViewModel : IDisposable
{
    private readonly AudioEvents _events;
    private readonly AudioDeviceWatcher _audioDeviceWatcher;
    private readonly IO io;
    private readonly IAudioManager _audioManager;
    private readonly INotificationService _notificationService;
    private readonly IDispatcher _dispatcher;
    private readonly IClipboard _clipboard;
    private readonly IApp _app;
    private readonly AudioSwitcher _audioSwitcher;
    private AudioDeviceClass _deviceClass = AudioDeviceClass.Render;
    [Property] private ObservableCollection<AudioDeviceViewModel> _devices = new();
    [Property] private ObservableCollection<AudioDeviceViewModel> _filteredDevices = new();
    [Property] private ObservableCollection<AudioCommandViewModel> _commands = new();
    [Property] private string _command = string.Empty;
    [Property] private AudioCommandViewModel _selectedCommand = new();
    [Property] private Hotkey _hotkey = new();
    [Property] private int _hotkeySelectionStart;
    [Property] private int _isExecuting = 1;
    private IList<object> _selectedDevices = new List<object>();

    public AudioPageViewModel(
        AudioSwitcher audioSwitcher,
        AudioEvents events,
        AudioDeviceWatcher watcher,
        IAudioManager audioManager,
        INotificationService notificationService,
        IO iO,
        IDispatcher dispatcherQueue,
        IClipboard clipboard,
        IApp app)
    {
        _audioSwitcher = audioSwitcher;
        _dispatcher = dispatcherQueue;
        _clipboard = clipboard;
        _app = app;
        _audioManager = audioManager;
        _notificationService = notificationService;
        _audioDeviceWatcher = watcher;
        _events = events;
        io = iO;
        InitializeCommands();
    }

    private IEnumerable<AudioDeviceViewModel> SelectedDevices => _selectedDevices.Cast<AudioDeviceViewModel>();

    public async Task InitializeAsync(AudioDeviceClass deviceClass, bool watch = true, IList<object>? selectedDevices = null)
    {
        _deviceClass = deviceClass;
        _events.Events += AudioDeviceWatcher_Events;
        _devices.CollectionChanged += Devices_CollectionChanged;
        if (selectedDevices != null)
            _selectedDevices = selectedDevices;

        await _audioSwitcher.LoadAsync();

        foreach (var command in _audioSwitcher.Commands
            .Where(x => x.DeviceClass == deviceClass)
            .Select(x => ToViewModel(x)))
        {
            Commands.Add(command);
        }

        if (watch)
            await StartAudioWatcherAsync();

        var selectedCommand = Commands.First();
        SelectDevices(Devices.Where(x => selectedCommand.DeviceIds.Contains(x.Id)));
        SelectedCommand = selectedCommand;
        --IsExecuting;
    }

    public Task StartAudioWatcherAsync() =>
        _audioDeviceWatcher.StartAsync(_deviceClass);

    public void Dispose()
    {
        _events.Events -= AudioDeviceWatcher_Events;
        _devices.CollectionChanged -= Devices_CollectionChanged;
        _audioDeviceWatcher.Dispose();
    }

    public void SaveSelectedCommand()
    {
        var command = SelectedCommand;
        command.DeviceIds = SelectedDevices.Select(x => x.Id).ToArray();
        _audioSwitcher.SaveCommand(ToModel(command));
    }

    [Command(CanExecuteMethod = nameof(CanExecute))]
    public async Task ToggleAsync()
    {
        try
        {
            await _audioSwitcher.ToggleAsync(_deviceClass, SelectedDevices.Select(x => x.Id), _notificationService);
        }
        catch (Exception e)
        {
            await io.ShowErrorAsync(e.Message);
        }
    }

    [Command(CanExecuteMethod = nameof(CanExecute))]
    public async Task NewAsync()
    {
        try
        {
            var name = await io.GetMessageAsync("Please enter a name");
            if (name == null)
                return;

            var cmd = _audioSwitcher.AddCommand(name.Trim(), _deviceClass);
            var command = new AudioCommandViewModel { Name = cmd.Name };
            Commands.Add(command);
            SelectedCommand = command;
        }
        catch (Exception e)
        {
            await io.ShowErrorAsync(e.Message);
        }
    }

    [Command(CanExecuteMethod = nameof(CanExecute))]
    public async Task RenameAsync()
    {
        try
        {
            var command = SelectedCommand;
            if (command == null)
                return;

            var newName = await io.GetMessageAsync("Please enter a new name", command.Name);
            if (newName == null)
                return;

            _audioSwitcher.RenameCommand(command.Name, newName.Trim());
            command.Name = newName;
        }
        catch (Exception e)
        {
            await io.ShowErrorAsync(e.Message);
        }
    }

    [Command(CanExecuteMethod = nameof(CanExecute))]
    public async Task DeleteAsync()
    {
        try
        {
            var command = SelectedCommand;
            if (command == null)
                return;

            _audioSwitcher.DeleteCommand(command.Name);
            var i = Commands.IndexOf(command);
            if (i != -1)
            {
                SelectedCommand = i + 1 < Commands.Count ? Commands[i + 1] : Commands[i - 1];
                Commands.RemoveAt(i);
            }
        }
        catch (Exception e)
        {
            await io.ShowErrorAsync(e.Message);
        }
    }

    [Command(CanExecuteMethod = nameof(CanExecute))]
    public async Task CopyCommandToClipboardAsync()
    {
        if (!SelectedDevices.Any())
        {
            await io.ShowErrorAsync("Please select one or more devices.");
            return;
        }

        var cmd = GetCmd();
        _clipboard.SetTextContent(cmd);

        var message = $"The following command has been copied to your clipboard:\n\n{cmd}\n\n" +
            "This command can be executed from the command line or using your favorite software to run it.\n";
        await io.ShowMessageAsync("Success", message);
    }

    public async Task ToggleDeviceVisibilityAsync(AudioDeviceViewModel device, bool sync = false)
    {
        try
        {
            var isDisabled = !_audioManager.IsDisabled(device.Id);
            var isEnabled = !isDisabled;
            _audioManager.SetVisibility(device.Id, isEnabled);
            if (sync)
                device.IsEnabled = isEnabled;
        }
        catch (Exception e)
        {
            await io.ShowErrorAsync(e.Message);
        }
    }

    public void SetAsDefault(string id, AudioDeviceRoleType role)
    {
        _audioManager.SetDefaultDevice(id, role);
    }

    public Command ToModel(AudioCommandViewModel cmd)
    {
        return new(cmd.Name, _deviceClass)
        {
            Action = CommandType.Set,
            Hotkey = cmd.Hotkey,
            Devices = SelectedDevices.Select(x => x.Id).ToArray(),
        };
    }

    public AudioCommandViewModel ToViewModel(Command command)
    {
        return new()
        {
            Name = command.Name,
            Hotkey = command.Hotkey,
            DeviceIds = command.Devices,
        };
    }

    public async Task TrySetHotkeyAsync(Hotkey hotkey, bool persist = true)
    {
        try
        {
            SetHotkey(hotkey, persist);
        }
        catch (Exception e)
        {
            await io.ShowErrorAsync(e.Message);
        }
    }

    public void SetHotkey(Hotkey hotkey, bool persist = true)
    {
        if (!hotkey.Modifiers.HasFlag(KeyModifiers.Control)
            && !hotkey.Modifiers.HasFlag(KeyModifiers.Menu))
        {
            hotkey = Hotkey.Empty;
        }

        if (persist)
        {
            var command = ToModel(SelectedCommand);
            _audioSwitcher.ChangeCommandHotkey(command, hotkey);
        }

        SelectedCommand.Hotkey = Hotkey = hotkey;
    }

    public IEnumerable<(string Name, Action Action, bool IsChecked, bool IsEnabled)> GetDeviceMenuOptions(AudioDeviceViewModel? device)
    {
        if (device != null)
        {
            yield return (_audioManager.IsDisabled(device.Id) ? "Enable" : "Disable", async () => await ToggleDeviceVisibilityAsync(device), false, true);
            yield return ("Set as Default Device", () => SetAsDefault(device.Id, AudioDeviceRoleType.Default), false, device.IsEnabled && !device.IsDefault);
            yield return ("Set as Default Communication Device", () => SetAsDefault(device.Id, AudioDeviceRoleType.Communications), false, device.IsEnabled && !device.IsDefaultCommunication);
            yield return (string.Empty, () => { }, false, true);
        }

        yield return ("Show Disabled Devices", ToggleShowDisabledDevices, _audioSwitcher.ShowDisabledDevices, true);
    }

    public void ToggleShowDisabledDevices()
    {
        _audioSwitcher.SetShowDisabledDevices(!_audioSwitcher.ShowDisabledDevices);
        UpdateFilteredDevices();
    }

    public string GetCmd()
    {
        return CLI.BuildCommand(_deviceClass, SelectedDevices.Select(x => new AudioDevice(x.Id, x.FullName)).ToArray());
    }

    public string GetCmdArgs()
    {
        return CLI.BuildCommandArgs(_deviceClass, SelectedDevices.Select(x => new AudioDevice(x.Id, x.FullName)).ToArray());
    }

    public async void CommandSelectionChanged()
    {
        using var executing = new ExecutingBlock(this);
        await LoadCommandAsync(SelectedCommand);
    }

    public async Task LoadCommandAsync(AudioCommandViewModel command)
    {
        SelectDevices(Devices.Where(x => command.DeviceIds.Contains(x.Id)));
        await TrySetHotkeyAsync(command.Hotkey, false);
        Command = GetCmd();
    }

    public void DeviceSelectionChanged()
    {
        if (!CanExecute())
            return;

        Command = GetCmd();
        SaveSelectedCommand();
        UpdateFilteredDevices();
    }

    public void SelectDevices(IEnumerable<AudioDeviceViewModel> devices)
    {
        _selectedDevices.Clear();

        foreach (var device in devices.Where(x => !_selectedDevices.Contains(x)))
            _selectedDevices.Add(device);
    }

    [CommandInvalidate(nameof(IsExecuting))]
    public bool CanExecute()
    {
        return IsExecuting == 0;
    }

    private void Devices_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateFilteredDevices();
    }

    private void UpdateFilteredDevices()
    {
        FilteredDevices.Remove(d => !Devices.Any(e => e.Id == d.Id));

        var orderedDevices = Devices.Where(x => Valid(x)).OrderByDescending(x => x.IsEnabled).ToArray();

        foreach (var device in Devices)
        {
            if (Valid(device))
            {
                var index = Math.Clamp(Array.IndexOf(orderedDevices, device), 0, FilteredDevices.Count);
                if (!FilteredDevices.Contains(device))
                    FilteredDevices.Insert(index, device);
            }
            else
            {
                if (FilteredDevices.Contains(device))
                    FilteredDevices.Remove(device);
            }
        }

        return;

        bool Valid(AudioDeviceViewModel model)
        {
            return _audioSwitcher.ShowDisabledDevices || model.IsEnabled || SelectedCommand.DeviceIds.Contains(model.Id);
        }
    }

    private void AudioDeviceWatcher_Events(object @event)
    {
        async void Handler()
        {
            switch (@event)
            {
                case DefaultAudioDeviceChanged defaultDeviceChanged:
                    OnEvent(defaultDeviceChanged);
                    break;
                case AudioDeviceAdded deviceAdded:
                    await OnEvent(deviceAdded);
                    break;
                case AudioDeviceUpdated deviceUpdated:
                    OnEvent(deviceUpdated);
                    break;
                case AudioDeviceRemoved deviceRemoved:
                    OnEvent(deviceRemoved);
                    break;
            }
        }

        _dispatcher.Enqueue(Handler);
    }

    private void OnEvent(DefaultAudioDeviceChanged e)
    {
        foreach (var deviceVm in Devices)
        {
            var role = e.Role;

            if (role == AudioDeviceRoleType.Default)
                deviceVm.IsDefault = e.Id == deviceVm.Id;
            else if (role == AudioDeviceRoleType.Communications)
                deviceVm.IsDefaultCommunication = e.Id == deviceVm.Id;
        }
    }

    private async Task OnEvent(AudioDeviceAdded e)
    {
        var device = e.Device;
        if (Devices.Any(x => x.Id == device.Id))
            return;

        var deviceVm = AudioDeviceViewModel.Create(_audioManager, device, _deviceClass);
        Devices.Add(deviceVm);
        try
        {
            var deviceInfo = await DeviceInformation.CreateFromIdAsync(device.Id);
            deviceVm.Img = await deviceInfo.GetThumbnailAsync();
        }
        catch
        {
        }
    }

    private void OnEvent(AudioDeviceUpdated e)
    {
        var changes = e.Changes;
        var deviceVm = Devices.FirstOrDefault(x => x.Id == e.Id);
        if (deviceVm != null)
        {
            deviceVm.Update(changes);
            UpdateFilteredDevices();
        }
    }

    private void OnEvent(AudioDeviceRemoved deviceRemoved)
    {
        Devices.Remove(x => x.Id == deviceRemoved.Id);
    }

    public sealed class ExecutingBlock : IDisposable
    {
        private readonly AudioPageViewModel model;

        public ExecutingBlock(AudioPageViewModel model)
        {
            this.model = model;
            ++model.IsExecuting;
        }

        public void Dispose()
        {
            --model.IsExecuting;
        }
    }
}
