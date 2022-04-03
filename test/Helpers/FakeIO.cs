namespace AudioDeviceSwitcher.Tests;

using Xunit.Abstractions;

public sealed class FakeIO : IO
{
    private readonly ITestOutputHelper output;
    public Queue<string> Queue = new();
    public List<string> Messages = new();

    public FakeIO(ITestOutputHelper output)
    {
        this.output = output;
    }

    public Task ShowMessageAsync(string title, string message)
    {
        Messages.Add(title);
        output.WriteLine(message);
        return Task.CompletedTask;
    }

    public Task<string?> GetMessageAsync(string message, string defValue)
    {
        return Task.FromResult<string?>(Queue.Dequeue());
    }
}
