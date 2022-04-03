namespace AudioDeviceSwitcher.Tests;

using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.DataTransfer;
using Xunit.Abstractions;

public sealed partial class AudioPageViewModelTests : ViewModelTest
{
    public AudioPageViewModelTests(ITestOutputHelper output) : base(output)
    {
    }

    [InlineData(AudioDeviceClass.Render)]
    [InlineData(AudioDeviceClass.Capture)]
    [UITheory]
    public async Task Main(AudioDeviceClass type)
    {
        var role = AudioDeviceRoleType.Default;
        var audioManager = Services.GetRequiredService<IAudioManager>();
        var cli = Services.GetRequiredService<CLI>();
        var initialDevice = audioManager.GetDefaultAudioId(type, role);

        try
        {
            using var model = await CreateViewModel(type);
            await model.StartAudioWatcherAsync();

            var devices = model.Devices
                .Where(x => x.IsEnabled)
                .OrderByDescending(x => x.IsDefault)
                .Take(2)
                .ToList();
            model.SelectDevices(devices);

            if (devices.Count < 2)
                throw new Exception($"Test requires at least 2 {type} devices. Found: {string.Join(";", devices.Select(x => x.FullName))}");

            await model.ToggleAsync();
            Assert.Equal(audioManager.GetDefaultAudioId(type, role), devices[1].Id);
            Assert.NotEqual(audioManager.GetDefaultAudioId(type, role), initialDevice);

            await model.ToggleAsync();
            Assert.Equal(audioManager.GetDefaultAudioId(type, role), initialDevice);

            await cli.RunAsync(model.GetCmdArgs());
            Assert.Equal(audioManager.GetDefaultAudioId(type, role), devices[1].Id);
            Assert.NotEqual(audioManager.GetDefaultAudioId(type, role), initialDevice);

            await cli.RunAsync(model.GetCmdArgs());
            Assert.Equal(audioManager.GetDefaultAudioId(type, role), initialDevice);

            foreach (var device in model.Devices.Where(x => x.IsEnabled))
            {
                model.SelectDevices(new[] { device });
                var cmd = model.GetCmdArgs();
                Assert.StartsWith("\"", cmd);
                await cli.RunAsync(cmd);
                Assert.True(audioManager.IsDefault(device.Id, type, role));
            }
        }
        finally
        {
            audioManager.SetDefaultDevice(initialDevice, role);
        }
    }

    [UIFact]
    public async Task New()
    {
        using var model = await CreateViewModel(AudioDeviceClass.Render);

        foreach (var name in new[] { "Default", "Default", "Default 2" })
        {
            AddInput(name);
            await model.NewAsync();

            Assert.Single(model.Commands, x => x.Name == name);
        }
    }

    [UIFact]
    public async Task Rename()
    {
        using var model = await CreateViewModel(AudioDeviceClass.Render);
        AddInput("Default 3");
        await model.NewAsync();

        AddInput("Default");
        await model.NewAsync();

        AddInput("Default 2");
        await model.RenameAsync();
        Assert.Equal("Default 2", model.SelectedCommand.Name);
        Assert.Single(model.Commands, x => x.Name == "Default 2");

        AddInput("Default 3");
        await model.RenameAsync();
        Assert.Equal("Default 2", model.SelectedCommand.Name);
    }

    [UIFact]
    public async Task Delete()
    {
        using var model = await CreateViewModel(AudioDeviceClass.Render);
        var def = model.SelectedCommand;

        var newCommandName = "Name";
        AddInput(newCommandName);
        await model.NewAsync();
        await model.DeleteAsync();

        Assert.DoesNotContain(model.Commands, x => x.Name == newCommandName);
        Assert.Same(model.SelectedCommand, def);
    }

    [UIFact]
    public async Task Delete_Last_Forbidden()
    {
        using var model = await CreateViewModel(AudioDeviceClass.Render);
        Assert.Contains(model.Commands, x => x.Name == "Default command");
        await model.DeleteAsync();
        Assert.Contains(model.Commands, x => x.Name == "Default command");
    }

    [UIFact]
    async Task Save()
    {
        using var model = await CreateViewModel(AudioDeviceClass.Render);

        AddInput("Default 2");
        await model.RenameAsync();
        var expectedHotKey = new Hotkey(KeyModifiers.Control, Key.A);
        model.SetHotkey(expectedHotKey);

        Assert.Equal("Default 2", model.SelectedCommand.Name);
        Assert.Equal(expectedHotKey, model.SelectedCommand.Hotkey);
        Assert.Equal(model.SelectedCommand.Hotkey, model.Hotkey);
    }

    [UIFact]
    async Task ShowDisabledDevice()
    {
        var services = CreateServideProvider(state: new AudioSwitcherState() { ShowDisabledDevices = true });
        var audioManager = services.GetRequiredService<IAudioManager>();
        var audioSwitcher = services.GetRequiredService<AudioSwitcher>();
        await audioSwitcher.LoadAsync();
        using var model = await CreateViewModel(AudioDeviceClass.Render);
        await model.StartAudioWatcherAsync();
        var count = model.FilteredDevices.Count();
        var device = model.Devices.First(x => !x.IsDisabled);

        try
        {
            await model.ToggleDeviceVisibilityAsync(device, sync: true);
            Assert.Equal(count, model.FilteredDevices.Count());
            Assert.Contains(device, model.FilteredDevices);

            model.ToggleShowDisabledDevices();
            Assert.DoesNotContain(device, model.FilteredDevices);
        }
        finally
        {
            audioManager.SetVisibility(device.Id, true);
        }
    }

    [UIFact]
    public async Task CopyCommandToClipboard()
    {
        using var model = await CreateViewModel(AudioDeviceClass.Render);
        await model.StartAudioWatcherAsync();

        model.SelectDevices(model.Devices.Take(2));
        await model.CopyCommandToClipboardAsync();
        var text = await Clipboard.GetContent().GetTextAsync();

        Assert.Equal(text, model.GetCmd());
    }

    private async Task<AudioPageViewModel> CreateViewModel(AudioDeviceClass type)
    {
        var model = Services.GetRequiredService<AudioPageViewModel>();
        await model.InitializeAsync(type, watch: false);
        return model;
    }
}
