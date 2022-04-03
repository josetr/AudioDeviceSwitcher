namespace AudioDeviceSwitcher.Tests;

using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

public class AudioSwitcherTests : Test
{
    public AudioSwitcherTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("", "Name cannot be empty.")]
    [InlineData("Default command", "Name 'Default command' is already in use.")]
    [InlineData("Name")]
    public void AddCommand(string name, string? expected = null)
    {
        var services = CreateServideProvider();
        var audioSwitcher = CreateAudioSwitcher(services);
        audioSwitcher.AddCommand("Default command", AudioDeviceClass.Render);

        try
        {
            audioSwitcher.AddCommand(name, AudioDeviceClass.Render);
        }
        catch (Exception e)
        {
            Assert.Equal(expected, e.Message);
            return;
        }

        Assert.Single(audioSwitcher.Commands, x => x.Name == name);
    }

    [Theory]
    [InlineData("cmd", "cmd2", "Command 'cmd' doesn't exist.")]
    [InlineData("command", "Default command", "Name 'Default command' is already in use.")]
    [InlineData("Default command", "Default command2")]
    public void RenameCommand(string name, string newName, string? expected = null)
    {
        var services = CreateServideProvider();
        var audioSwitcher = CreateAudioSwitcher(services);
        audioSwitcher.AddCommand("Default command", AudioDeviceClass.Render);
        audioSwitcher.AddCommand("command", AudioDeviceClass.Render);

        try
        {
            audioSwitcher.RenameCommand(name, newName);
        }
        catch (Exception e)
        {
            Assert.Equal(expected, e.Message);
            return;
        }

        Assert.DoesNotContain(audioSwitcher.Commands, x => x.Name == name);
        Assert.Single(audioSwitcher.Commands, x => x.Name == newName);
    }

    [Theory]
    [InlineData("Default command", true, "Command list cannot be left empty.")]
    [InlineData("Default command2", false, "Command 'Default command2' doesn't exist.")]
    [InlineData("Default command", false)]
    public void DeleteCommand(string name, bool single, string? expected = null)
    {
        var services = CreateServideProvider();
        var audioSwitcher = CreateAudioSwitcher(services);
        audioSwitcher.AddCommand("Default command", AudioDeviceClass.Render);
        if (!single)
            audioSwitcher.AddCommand("Command", AudioDeviceClass.Render);

        try
        {
            audioSwitcher.DeleteCommand(name);
        }
        catch (Exception e)
        {
            Assert.Equal(expected, e.Message);
            return;
        }

        Assert.DoesNotContain(audioSwitcher.Commands, x => x.Name == name);
    }

    [Theory]
    [InlineData(Key.Z, AudioDeviceState.Active, true, true, $"Command with hotkey 'Ctrl + Z' doesn't exist.")]
    [InlineData(Key.A, AudioDeviceState.Disabled, false, true, "⚠️ Skipped 'Speakers' because it is disabled.")]
    [InlineData(Key.A, AudioDeviceState.Active, false, true, "⚠️ Skipped 'Speakers' because it may not exist.")]
    [InlineData(Key.A, AudioDeviceState.Active, true, false, "⚠️ Skipped 'Speakers' because it may be disconnected.")]
    [InlineData(Key.A, AudioDeviceState.Active, true, true, "✔️ Speakers")]
    public async Task ExecuteCommandByHotkey(Key key, AudioDeviceState state, bool exists, bool connected, string? expected = null)
    {
        var audioManager = new Mock<IAudioManager>();
        var headphones = new AudioDevice("12345", "Headphones");
        var speakers = new AudioDevice("1234", "Speakers");
        var role = AudioDeviceRoleType.Default;
        var type = AudioDeviceClass.Render;
        var notificationService = new Mock<INotificationService>();
        var services = CreateServideProvider(null, x =>
        {
            x.AddSingleton(notificationService.Object);
            x.AddSingleton(audioManager.Object);
        });
        var audioSwitcher = CreateAudioSwitcher(services);

        audioManager.Setup(x => x.GetAllDevicesAsync(type))
            .ReturnsAsync(new[] { headphones, speakers });
        audioManager.Setup(x => x.SetDefaultDevice(speakers.Id, role))
            .Returns(exists);
        audioManager.Setup(x => x.GetState(speakers.Id))
            .Returns(state);

        if (connected)
        {
            audioManager.SetupSequence(x => x.GetDefaultAudioId(type, role))
                .Returns(headphones.Id)
                .Returns(speakers.Id);
        }

        var command = audioSwitcher.AddCommand("Default command", type, new Hotkey(KeyModifiers.Control, Key.A), new[] { speakers.Id });

        try
        {
            await audioSwitcher.ExecuteCommandByHotkeyAsync(new Hotkey(KeyModifiers.Control, key));
        }
        catch (Exception e)
        {
            Assert.Equal(expected, e.Message);
            return;
        }

        if (state.HasFlag(AudioDeviceState.Active))
            audioManager.Verify(x => x.SetDefaultDevice(speakers.Id, role), Times.Once());

        notificationService.Verify(x => x.ShowNotificationAsync(expected ?? string.Empty), Times.Once());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetRunAtStartupTask(bool value)
    {
        var services = CreateServideProvider();
        var audioSwitcher = CreateAudioSwitcher(services);
        await audioSwitcher.SetRunAtStartupTask(value);
        await audioSwitcher.LoadAsync();
        Assert.Equal(value, audioSwitcher.RunAtStartup);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetRunAtStartupMinimized(bool value)
    {
        var services = CreateServideProvider();
        var audioSwitcher = CreateAudioSwitcher(services);
        audioSwitcher.SetRunAtStartupMinimized(value);
        await audioSwitcher.LoadAsync();
        Assert.Equal(value, audioSwitcher.RunAtStartupMinimized);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetRunInBackground(bool value)
    {
        var services = CreateServideProvider();
        var audioSwitcher = CreateAudioSwitcher(services);
        audioSwitcher.SetRunInBackground(value);
        await audioSwitcher.LoadAsync();
        Assert.Equal(value, audioSwitcher.RunInBackground);
    }

    [UITheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetSwitchCommunicationDevice(bool value)
    {
        var services = CreateServideProvider();
        var audioSwitcher = CreateAudioSwitcher(services);
        audioSwitcher.SetSwitchCommunicationDevice(value);
        await audioSwitcher.LoadAsync();
        Assert.Equal(value, audioSwitcher.SwitchCommunicationDevice);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetShowDisabledDevices(bool value)
    {
        var services = CreateServideProvider();
        var audioSwitcher = CreateAudioSwitcher(services);
        audioSwitcher.SetShowDisabledDevices(value);
        await audioSwitcher.LoadAsync();
        Assert.Equal(value, audioSwitcher.ShowDisabledDevices);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetDarkTheme(bool value)
    {
        var services = CreateServideProvider();
        var audioSwitcher = CreateAudioSwitcher(services);
        audioSwitcher.SetDarkTheme(value);
        await audioSwitcher.LoadAsync();
        Assert.Equal(value, audioSwitcher.DarkTheme);
    }

    private AudioSwitcher CreateAudioSwitcher(IServiceProvider provider)
    {
        return provider.GetRequiredService<AudioSwitcher>();
    }
}
