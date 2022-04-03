namespace AudioDeviceSwitcher.Tests;

public sealed class FakeStartupTask : IStartupTask
{
    private bool enabled = false;

    public Task DisableAsync()
    {
        enabled = false;
        return Task.CompletedTask;
    }

    public Task EnableAsync()
    {
        enabled = true;
        return Task.CompletedTask;
    }

    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(enabled);
    }
}