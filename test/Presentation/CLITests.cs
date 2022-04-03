namespace AudioDeviceSwitcher.Tests;

public class CLITests
{
    [Theory]
    [InlineData("", "id", "Speakers", "id", "Headphones", "Invalid device name. Please regenerate your command.")]
    [InlineData("1", "id", "Speakers", "id", "Headphones", "Device \"1\" doesn't exist. Please regenerate your command.")]
    [InlineData("Speakers / 177612", "id", "Speakers", "id", "Headphones", "Found two devices with the same name. Please regenerate your command.")]
    [InlineData("Speakers / 177612", "id", "Speakers", "id2", "Speakers", "Found two devices with the same name. Please regenerate your command.")]
    [InlineData("Speakers / 177612", "id", "Speakers", "id2", "Headphones")]
    void GetDeviceId(string text, string id, string name, string id2, string name2, string? expected = null)
    {
        try
        {
            Assert.Equal("id", CLI.GetDeviceId(text, new[] {
                new AudioDevice(id, name),
                new AudioDevice(id2, name2) }));
        }
        catch (Exception e)
        {
            Assert.Equal(expected, e.Message);
        }
    }

    [Theory]
    [InlineData("\"arg 1\" \"arg 2\"", "arg 1", "arg 2")]
    void ParseArguments(string cmd, params string[] expected)
    {
        Assert.Equal(expected, CLI.ParseArguments(cmd));
    }

    [Theory]
    [InlineData(AudioDeviceClass.Render, "\"name / -?\\d+\" \"name2 / -?\\d+\" -playback", "name", "name2")]
    [InlineData(AudioDeviceClass.Render, "\"name / -?\\d+\" -playback", "name")]
    [InlineData(AudioDeviceClass.Capture, "\"name / -?\\d+\" -recording", "name")]
    public void BuildCommand(AudioDeviceClass type, string expected, params string[] selectedDevices)
    {
        var devices = selectedDevices.Select(x => new AudioDevice(x, x));
        var result = CLI.BuildCommand(type, devices.ToArray());
        Assert.Matches("^AudioDeviceSwitcher " + expected + "$", result);
    }
}
