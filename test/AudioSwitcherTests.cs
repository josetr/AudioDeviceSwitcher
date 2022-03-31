namespace AudioDeviceSwitcher.Tests;

using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Enumeration;
using Windows.System;
using Xunit;

public class AudioSwitcherTests
{
    [InlineData(DeviceClass.AudioRender)]
    [InlineData(DeviceClass.AudioCapture)]
    [UITheory]
    public async Task Main(DeviceClass type)
    {
        var initialDevice = AudioUtil.GetDefaultAudioId(type);

        try
        {
            using var page = new AudioPageViewModel(new(), type);
            await page.LoadDevices();

            var devices = page.Devices
                .Where(x => x.IsEnabled)
                .OrderByDescending(x => x.IsDefault)
                .Take(2)
                .ToList();
            page.SelectDevices(devices);

            if (devices.Count < 2)
                throw new Exception($"Test requires at least 2 {type} devices. Found: {string.Join(";", devices.Select(x => x.FullName)) }");

            var count = page.Devices.Count;

            await page.ToggleAsync();
            Assert.Equal(AudioUtil.GetDefaultAudioId(type), devices[1].Id);
            Assert.NotEqual(AudioUtil.GetDefaultAudioId(type), initialDevice);

            await page.ToggleAsync();
            Assert.Equal(AudioUtil.GetDefaultAudioId(type), initialDevice);

            await CLI.RunAsync(page.GetCmdArgs());
            Assert.Equal(AudioUtil.GetDefaultAudioId(type), devices[1].Id);
            Assert.NotEqual(AudioUtil.GetDefaultAudioId(type), initialDevice);

            await CLI.RunAsync(page.GetCmdArgs());
            Assert.Equal(AudioUtil.GetDefaultAudioId(type), initialDevice);

            foreach (var device in page.Devices.Where(x => x.IsEnabled))
            {
                page.ClearSelection();
                page.SelectDevices(new[] { device });
                var cmd = page.GetCmdArgs();
                Assert.StartsWith("\"", cmd);
                await CLI.RunAsync(cmd);
                Assert.True(AudioUtil.IsDefault(device.Id, type));
            }
        }
        finally
        {
            AudioUtil.SetDefaultDevice(initialDevice);
        }
    }

    [UIFact]
    public async Task New()
    {
        var audioSwitcher = new AudioSwitcher();
        using var page = new AudioPageViewModel(audioSwitcher, DeviceClass.AudioRender) { IO = io };

        foreach (var name in new[] { "Default", "Default", "Default 2" })
        {
            AddInput(page, name);
            await page.NewAsync();

            Assert.Single(page.Commands, x => x.Name == name);
            Assert.Single(audioSwitcher.Commands, x => x.Name == name);
        }
    }

    [UIFact]
    public async Task Rename()
    {
        using var page = new AudioPageViewModel(new(), DeviceClass.AudioRender) { IO = io };
        AddInput(page, "Default 3");
        await page.NewAsync();

        AddInput(page, "Default");
        await page.NewAsync();
        Assert.Contains(page.Commands, x => x.Name == "Default");

        AddInput(page, "Default 2");
        await page.RenameAsync();
        Assert.Equal("Default 2", page.SelectedCommand.Name);

        AddInput(page, "Default 3");
        await page.RenameAsync();
        Assert.Equal("Default 2", page.SelectedCommand.Name);
    }

    [UIFact]
    public async Task Delete()
    {
        using var model = new AudioPageViewModel(new(), DeviceClass.AudioRender) { IO = io };
        model.LoadCommands();

        var def = model.SelectedCommand;
        Assert.Contains(model.Commands, x => x.Name == "Default command");
        await model.DeleteAsync();

        Assert.Contains(model.Commands, x => x.Name == "Default command");

        AddInput(model, "Name");
        await model.NewAsync();
        await model.DeleteAsync();

        Assert.DoesNotContain(model.Commands, x => x.Name == "Name");
        Assert.Same(model.SelectedCommand, def);
    }

    [UIFact]
    async Task Save()
    {
        using var page = new AudioPageViewModel(new(), DeviceClass.AudioRender) { IO = io };
        page.LoadCommands();

        AddInput(page, "Default 2");
        await page.RenameAsync();
        var expectedHotKey = new Hotkey(VirtualKeyModifiers.Control, VirtualKey.A);
        await page.SetHotkeyAsync(expectedHotKey);

        Assert.Equal("Default 2", page.SelectedCommand.Name);
        Assert.Equal(expectedHotKey, page.SelectedCommand.Hotkey);
        Assert.Equal(page.SelectedCommand.Hotkey, page.Hotkey);
    }

    [UIFact]
    async Task ShowDisabledDevice()
    {
        using var page = new AudioPageViewModel(new(new() { ShowDisabledDevices = true }), DeviceClass.AudioRender) { IO = io };
        await page.LoadDevices();
        var count = page.FilteredDevices.Count();
        var device = page.Devices.First(x => !x.IsDisabled);

        try
        {
            await page.ToggleDeviceVisibilityAsync(device, sync: true);
            Assert.Equal(count, page.FilteredDevices.Count());
            Assert.Contains(device, page.FilteredDevices);

            page.ShowDisabledDevices();
            Assert.DoesNotContain(device, page.FilteredDevices);
        }
        finally
        {
            AudioUtil.SetVisibility(device.Id, true);
        }
    }

    [UIFact]
    public async Task CopyCommandToClipboard()
    {
        using var page = new AudioPageViewModel(new(), DeviceClass.AudioRender);
        await page.LoadDevices();

        page.SelectDevices(page.Devices.Take(2));
        await page.CopyCommandToClipboardAsync();
        var text = await Clipboard.GetContent().GetTextAsync();

        Assert.Equal(text, page.GetCmd());
    }

    void AddInput(AudioPageViewModel page, string msg) => io.Queue.Enqueue(msg);

    TestIO io = new();

    class TestIO : IO
    {
        public Queue<string> Queue = new();

        public Task ShowMessageAsync(string title, string message)
        {
            return Task.CompletedTask;
        }

        public Task<string?> GetMessageAsync(string message, string defValue)
        {
            return Task.FromResult<string?>(Queue.Dequeue());
        }

        public Task ShowNotification(string message)
        {
            return Task.CompletedTask;
        }
    }
}
