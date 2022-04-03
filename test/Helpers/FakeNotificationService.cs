namespace AudioDeviceSwitcher.Tests;

using Xunit.Abstractions;

public sealed class FakeNotificationService : INotificationService
{
    private readonly ITestOutputHelper output;

    public FakeNotificationService(ITestOutputHelper output)
    {
        this.output = output;
    }

    public Task ShowNotificationAsync(string message)
    {
        output.WriteLine(message);
        return Task.CompletedTask;
    }
}
