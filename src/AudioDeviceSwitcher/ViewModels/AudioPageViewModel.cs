// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.InteropServices.WindowsRuntime;
using AudioDeviceSwitcher.Interop;
using MvvmGen;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using Windows.System;

[ViewModel(GenerateConstructor = false)]
public sealed partial class AudioPageViewModel : IDisposable
{
    [Property] private ObservableCollection<AudioDeviceViewModel> _devices = new() { };
    [Property] private ObservableCollection<AudioDeviceViewModel> _filteredDevices = new() { };
    [Property] private ObservableCollection<AudioCommandViewModel> _commands = new() { };
    [Property] private string _command = string.Empty;
    [Property] private AudioCommandViewModel _selectedCommand = new();
    [Property] private Hotkey _hotkey = new();
    [Property] private int _hotkeySelectionStart;
    private IList<object> _selectedDevices = new List<object>();
    private AudioSwitcher _audioSwitcher;
    private AudioDeviceWatcher _audioDeviceWatcher;
    private bool _loading = false;

    public AudioPageViewModel(AudioSwitcher? audioSwitcher = null, DeviceClass deviceClass = DeviceClass.AudioRender)
    {
        _audioSwitcher = audioSwitcher ?? new();
        _audioDeviceWatcher = new(this);
        _devices.CollectionChanged += Devices_CollectionChanged;
        DeviceClass = deviceClass;
        InitializeCommands();
    }

    public DeviceClass DeviceClass { get; set; } = DeviceClass.AudioRender;
    public Microsoft.UI.Dispatching.DispatcherQueue? DispatcherQueue { get; set; }
    public IO IO { get; set; } = new NullIO();
    public IEnumerable<AudioDeviceViewModel> SelectedDevices => _selectedDevices.Cast<AudioDeviceViewModel>();

    private Settings Settings => _audioSwitcher.Settings;

    public Task LoadDevices()
    {
        using var loading = new LoadingBlock(this);
        return _audioDeviceWatcher.Start();
    }

    public void Dispose()
    {
        _audioDeviceWatcher.Dispose();
    }

    public void LoadCommands()
    {
        using var loading = new LoadingBlock(this);

        _audioSwitcher.Load();
        Commands.Clear();

        foreach (var command in _audioSwitcher.Commands.Where(x => x.DeviceClass == DeviceClass))
            Commands.Add(ToViewModel(command));

        SelectedCommand = Commands.FirstOrDefault();
        _audioSwitcher.RegisterHotkeys();
    }

    public void SaveSelectedCommand()
    {
        if (_loading)
            return;

        var command = SelectedCommand;
        if (command == null)
            return;

        command.DeviceIds = SelectedDevices.Select(x => x.Id).ToArray();
        var index = _audioSwitcher.Commands.FindIndex(x => x.Name == command.Name);
        if (index < 0)
            return;

        _audioSwitcher.Commands[index] = ToModel(command);
        _audioSwitcher.SaveSettings();
        _audioSwitcher.RegisterHotkeys();
    }

    [Command]
    public async Task ToggleAsync()
    {
        try
        {
            await _audioSwitcher.ToggleAsync(DeviceClass, SelectedDevices.Select(x => x.Id), Settings.SwitchCommunicationDevice);
        }
        catch (Exception e)
        {
            await IO.ShowErrorAsync(e.Message);
        }
    }

    [Command]
    public async Task NewAsync()
    {
        var name = await IO.GetMessageAsync("Please enter a name");
        if (name == null)
            return;

        try
        {
            var cmd = _audioSwitcher.AddCommand(name, DeviceClass);
            var command = new AudioCommandViewModel { Name = cmd.Name };
            Commands.Add(command);
            SelectedCommand = command;
        }
        catch (Exception e)
        {
            await IO.ShowErrorAsync(e.Message);
        }
    }

    [Command]
    public async Task RenameAsync()
    {
        var command = SelectedCommand;
        if (command == null)
            return;

        var newName = await IO.GetMessageAsync("Please enter a new name", command.Name);
        if (newName == null)
            return;

        try
        {
            _audioSwitcher.RenameCommand(command.Name, newName);
            command.Name = newName;
        }
        catch (Exception e)
        {
            await IO.ShowErrorAsync(e.Message);
        }
    }

    [Command]
    public async Task DeleteAsync()
    {
        var command = SelectedCommand;
        if (command == null)
            return;

        try
        {
            _audioSwitcher.DeleteCommand(command.Name);

            for (int i = 0; i < Commands.Count; ++i)
            {
                if (Commands[i] != command)
                    continue;

                Commands.Remove(command);
                SelectedCommand = i < Commands.Count ? Commands[i] : Commands.LastOrDefault();
                return;
            }
        }
        catch (Exception e)
        {
            await IO.ShowErrorAsync(e.Message);
        }
    }

    [Command]
    public async Task CopyCommandToClipboardAsync()
    {
        var cmd = GetCmd();
        if (string.IsNullOrWhiteSpace(cmd))
        {
            await IO.ShowErrorAsync("Please select one or more devices");
            return;
        }

        var content = new DataPackage();
        content.SetText(cmd);
        Clipboard.SetContent(content);
        await IO.ShowMessageAsync("Success", $"The following command has been copied to your clipboard:\n\n{cmd}\n\nThis command can be executed from the command line or using your favorite software to run it.\n");
    }

    public async Task ToggleDeviceVisibilityAsync(AudioDeviceViewModel device, bool sync = false)
    {
        try
        {
            var isDisabled = !AudioUtil.IsDisabled(device.Id);
            var isEnabled = !isDisabled;
            AudioUtil.SetVisibility(device.Id, isEnabled);
            if (sync)
                device.IsEnabled = isEnabled;
        }
        catch (Exception e)
        {
            await IO.ShowErrorAsync(e.Message);
        }
    }

    public void SetAsDefault(string id)
    {
        AudioUtil.SetDefaultDevice(id);
    }

    public void SetAsDefaultCommunication(string id)
    {
        AudioUtil.SetDefaultDevice(id, ERole.eCommunications);
    }

    public Command ToModel(AudioCommandViewModel cmd)
    {
        return new(cmd.Name, DeviceClass)
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

    public async Task SetHotkeyAsync(Hotkey hotkey)
    {
        if (!hotkey.Modifiers.HasFlag(VirtualKeyModifiers.Control)
            && !hotkey.Modifiers.HasFlag(VirtualKeyModifiers.Menu))
        {
            hotkey = Hotkey.Empty;
        }

        if (hotkey != Hotkey.Empty)
        {
            var cmd = _audioSwitcher.Commands.FirstOrDefault(x => x.Hotkey == hotkey);
            if (cmd != null && cmd.Name != SelectedCommand?.Name)
            {
                if (cmd.DeviceClass == DeviceClass)
                {
                    await IO.ShowErrorAsync($"'{hotkey}' is already in use by command '{cmd.Name}'");
                    return;
                }
            }
        }

        if (SelectedCommand == null)
            return;

        SelectedCommand.Hotkey = Hotkey = hotkey;
        SaveSelectedCommand();
    }

    public string GetCmd()
    {
        if (!SelectedDevices.Any())
            return string.Empty;

        return $"AudioDeviceSwitcher {GetCmdArgs()}";
    }

    public IEnumerable<(string Name, Action Action, bool IsChecked, bool IsEnabled)> GetDeviceMenuOptions(AudioDeviceViewModel? device)
    {
        if (device != null)
        {
            yield return (AudioUtil.IsDisabled(device.Id) ? "Enable" : "Disable", async () => await ToggleDeviceVisibilityAsync(device), false, true);
            yield return ("Set as Default Device", () => SetAsDefault(device.Id), false, device.IsEnabled && !device.IsDefault);
            yield return ("Set as Default Communication Device", () => SetAsDefaultCommunication(device.Id), false, device.IsEnabled && !device.IsDefaultCommunication);
            yield return (string.Empty, () => { }, false, true);
        }

        yield return ("Show Disabled Devices", ShowDisabledDevices, _audioSwitcher.Settings.ShowDisabledDevices, true);
    }

    public void ShowDisabledDevices()
    {
        _audioSwitcher.Settings.ShowDisabledDevices = !_audioSwitcher.Settings.ShowDisabledDevices;
        _audioSwitcher.Settings.Save();
        UpdateFilteredDevices();
    }

    public string GetCmdArgs()
    {
        var devices = SelectedDevices.Select(x => new Device() { Name = x.FullName, Id = x.Id });
        return AudioSwitcher.GetCommandArgs(DeviceClass, devices.ToArray());
    }

    public async void CommandSelectionChanged()
    {
        if (SelectedCommand != null)
            await LoadCommand(SelectedCommand);
    }

    public async Task LoadCommand(AudioCommandViewModel command)
    {
        using var loading = new LoadingBlock(this);
        ClearSelection();

        await SetHotkeyAsync(command.Hotkey);
        SelectDevices(Devices.Where(x => command.DeviceIds.Contains(x.Id)));
        Command = GetCmd();
    }

    [Command]
    public void DeviceSelectionChanged()
    {
        Command = GetCmd();
        SaveSelectedCommand();
        UpdateFilteredDevices();
    }

    public void SelectDevices(IEnumerable<AudioDeviceViewModel> devices)
    {
        foreach (var device in devices)
        {
            if (!_selectedDevices.Contains(device))
                _selectedDevices.Add(device);
        }
    }

    public void ClearSelection()
    {
        _selectedDevices.Clear();
    }

    internal void SwapSelectedDevicesContainer(IList<object> newContainer)
    {
        if (_selectedDevices == newContainer)
            return;

        using var loading = new LoadingBlock(this);
        var devices = _selectedDevices;
        _selectedDevices = newContainer;
        SelectDevices(devices.Cast<AudioDeviceViewModel>());
    }

    private void Devices_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (AudioDeviceViewModel device in e.OldItems)
                _selectedDevices.Remove(device);
        }

        if (e.NewItems != null)
        {
            if (SelectedCommand != null)
                SelectDevices(e.NewItems.Cast<AudioDeviceViewModel>().Where(x => SelectedCommand.DeviceIds.Contains(x.Id)));
        }

        UpdateFilteredDevices();
    }

    private void UpdateFilteredDevices()
    {
        FilteredDevices.Remove(d => !Devices.Any(e => e.Id == d.Id));

        bool Valid(AudioDeviceViewModel model)
        {
            return Settings.ShowDisabledDevices || model.IsEnabled || SelectedCommand.DeviceIds.Contains(model.Id);
        }

        var order = Devices.Where(x => Valid(x)).OrderByDescending(x => x.IsEnabled);

        foreach (var device in Devices)
        {
            if (Valid(device))
            {
                var index = 0;
                foreach (var d in order)
                {
                    if (d == device)
                        break;
                    index++;
                }

                if (index > FilteredDevices.Count)
                    index = FilteredDevices.Count;

                if (!FilteredDevices.Contains(device))
                    FilteredDevices.Insert(index, device);
            }
            else
            {
                if (FilteredDevices.Contains(device))
                    FilteredDevices.Remove(device);
            }
        }
    }

    private void TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueueHandler handler)
    {
        if (DispatcherQueue == null)
            handler();
        else
            DispatcherQueue?.TryEnqueue(handler);
    }

    private sealed class LoadingBlock : IDisposable
    {
        private AudioPageViewModel model;

        public LoadingBlock(AudioPageViewModel model)
        {
            this.model = model;
            model._loading = true;
        }

        public void Dispose()
        {
            model._loading = false;
        }
    }

    private class AudioDeviceWatcher : IDisposable
    {
        private readonly SemaphoreSlim enumerationCompletedSignal = new SemaphoreSlim(0, 1);
        private readonly AudioPageViewModel model;
        private DeviceWatcher? deviceWatcher = null;
        private bool started = false;

        public AudioDeviceWatcher(AudioPageViewModel model)
        {
            this.model = model;
        }

        public DeviceClass DeviceClass => model.DeviceClass;
        public ObservableCollection<AudioDeviceViewModel> Devices => model.Devices;

        public Task Start()
        {
            if (started)
                return Task.CompletedTask;

            started = true;

            if (DeviceClass == DeviceClass.AudioRender)
                MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
            else if (DeviceClass == DeviceClass.AudioCapture)
                MediaDevice.DefaultAudioCaptureDeviceChanged += MediaDevice_DefaultAudioCaptureDeviceChanged;

            if (deviceWatcher == null)
            {
                deviceWatcher = DeviceInformation.CreateWatcher(AudioUtil.GetInterfaceGuid(DeviceClass));
                deviceWatcher.EnumerationCompleted += Watcher_EnumerationCompleted;
                deviceWatcher.Added += Watcher_Added;
                deviceWatcher.Removed += Watcher_Removed;
                deviceWatcher.Updated += Watcher_Updated;
            }

            deviceWatcher.Start();
            return enumerationCompletedSignal.WaitAsync();
        }

        public void Stop()
        {
            if (!started)
                return;

            started = false;
            deviceWatcher?.Stop();

            if (DeviceClass == DeviceClass.AudioRender)
                MediaDevice.DefaultAudioRenderDeviceChanged -= MediaDevice_DefaultAudioRenderDeviceChanged;
            else if (DeviceClass == DeviceClass.AudioCapture)
                MediaDevice.DefaultAudioCaptureDeviceChanged -= MediaDevice_DefaultAudioCaptureDeviceChanged;
        }

        public void Dispose()
        {
            Stop();
            deviceWatcher = null;
        }

        private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            MediaDevice_DefaultAudioDeviceChanged(args.Role, args.Id);
        }

        private void MediaDevice_DefaultAudioCaptureDeviceChanged(object sender, DefaultAudioCaptureDeviceChangedEventArgs args)
        {
            MediaDevice_DefaultAudioDeviceChanged(args.Role, args.Id);
        }

        private void MediaDevice_DefaultAudioDeviceChanged(AudioDeviceRole role, string id)
        {
            model.TryEnqueue(() =>
            {
                foreach (var device in Devices)
                    device.UpdateDefault(role, id);
            });
        }

        private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            model.TryEnqueue(async () =>
            {
                if (Devices.Any(x => x.Id == args.Id))
                    return;

                var device = AudioDeviceViewModel.Create(args, DeviceClass);
                Devices.Add(device);
                await device.LoadImageAsync(args);
            });
        }

        private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            model.TryEnqueue(() =>
            {
                Devices.FirstOrDefault(x => x.Id == args.Id)?.Update(args);
                model.UpdateFilteredDevices();
            });
        }

        private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            model.TryEnqueue(() => Devices.Remove(x => x.Id == args.Id));
        }

        private void Watcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            model.TryEnqueue(() => enumerationCompletedSignal.Release());
        }
    }
}
