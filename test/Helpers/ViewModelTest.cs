namespace AudioDeviceSwitcher.Tests;

using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

public class ViewModelTest : Test
{
    public ViewModelTest(ITestOutputHelper output) : base(output)
    {
    }

    public void AddInput(string msg)
    {
        var io = (FakeIO)Services.GetRequiredService<IO>();
        io.Queue.Enqueue(msg);
    }

    protected override IServiceProvider _CreateServideProvider()
    {
        overwriteServices = services =>
        {
            services.AddSingleton<IO>(new FakeIO(output));
            services.AddSingleton<INotificationService>(new FakeNotificationService(output));
            services.AddSingleton<IAudioManager>(new AudioManager());
            services.AddSingleton<IDispatcher, SyncDispatcher>();
            services.AddSingleton<IClipboard, WinClipboard>();
            services.AddSingleton<AudioSwitcher>();
            services.AddSingleton<AudioEvents>();
            services.AddSingleton<AudioDeviceWatcher>();
            services.AddSingleton<CLI>();
            services.AddSingleton<AudioPageViewModel>();
            services.AddSingleton<SettingsPageViewModel>();
        };

        return base._CreateServideProvider();
    }
}
