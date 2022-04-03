namespace AudioDeviceSwitcher.Tests;

using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit.Abstractions;

public class Test
{
    private readonly Lazy<IServiceProvider> _services;
    protected readonly ITestOutputHelper output;
    protected AudioSwitcherState? state;
    protected IServiceProvider Services => _services.Value;
    protected Action<ServiceCollection>? overwriteServices;

    public Test(ITestOutputHelper output)
    {
        this.output = output;
        _services = new Lazy<IServiceProvider>(() => _CreateServideProvider());
    }

    public IServiceProvider CreateServideProvider(AudioSwitcherState? state = null, Action<ServiceCollection>? services = null)
    {
        this.state ??= state;
        this.overwriteServices ??= services;
        return Services;
    }

    protected virtual IServiceProvider _CreateServideProvider()
    {
        var stateStorage = new InMemoryStateStorage();
        if (state != null)
            stateStorage.Save(state);

        var startupTaskManager = new FakeStartupTaskManager();
        startupTaskManager.Tasks.Add(AudioSwitcher.StartupTaskId, new FakeStartupTask());

        var services = new ServiceCollection();
        services.AddSingleton<IStartupTaskManager>(startupTaskManager);
        services.AddSingleton<IStateStorage>(stateStorage);
        services.AddSingleton<IO>(new NoIO());
        services.AddSingleton(new Mock<INotificationService>().Object);
        services.AddSingleton(new Mock<IHotkeyManager>().Object);
        services.AddSingleton(new Mock<IAudioManager>().Object);
        services.AddSingleton(new Mock<IApp>().Object);
        services.AddSingleton(new Mock<IClipboard>().Object);
        services.AddSingleton(new Mock<IDispatcher>().Object);
        services.AddSingleton<AudioSwitcher>();
        services.AddSingleton<CLI>();
        overwriteServices?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
