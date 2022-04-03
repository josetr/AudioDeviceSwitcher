namespace AudioDeviceSwitcher.Tests;

public sealed class FakeStartupTaskManager : IStartupTaskManager
{
    public Dictionary<string, IStartupTask> Tasks { get; } = new();

    public Task<IStartupTask> GetTaskAsync(string id)
    {
        return Task.FromResult(Tasks[id]);
    }
}
