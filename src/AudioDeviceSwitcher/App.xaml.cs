// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;
using Activation = Windows.ApplicationModel.Activation;
using PInvoke;
using Microsoft.UI.Xaml.Controls;
using System.Reflection.Metadata.Ecma335;

public sealed partial class App : Application, IApp
{
    public const string Id = "JoseTorres:AudioDeviceSwitcher";
    private static Kernel32.SafeObjectHandle? _mutex;
    private static MainWindow? _window;

    static App()
    {
        Hotkey.KeyPrinter = key => key.ToVirtualKey().ToString();
    }

    public App()
    {
        Services = ConfigureServices();
        InitializeComponent();
    }

    public static AudioSwitcher AudioSwitcher { get; set; } = default!;
    public static MainWindow Window => _window!;
    public static ElementTheme Theme => AudioSwitcher.DarkTheme ? ElementTheme.Dark : ElementTheme.Light;
    public IServiceProvider Services { get; }
    public static new App Current => (App)Application.Current;

    public void UpdateTheme()
    {
        if (_window?.Content is FrameworkElement e)
            e.RequestedTheme = Theme;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var cmdArgs = Environment.GetCommandLineArgs();
        if (cmdArgs.Length >= 2)
        {
            try
            {
                var storage = Services.GetRequiredService<IStateStorage>();
                var audioManager = Services.GetRequiredService<IAudioManager>();
                var cli = new CLI(audioManager, storage, new NoNotificationService());
                await cli.RunAsync(cmdArgs.Skip(1).ToArray());
            }
            catch (Exception e)
            {
                User32.MessageBox(IntPtr.Zero, e.Message, AudioSwitcherState.Title, User32.MessageBoxOptions.MB_ICONERROR);
            }

            ExitNow();
            return;
        }

        _mutex = Kernel32.CreateMutex(IntPtr.Zero, false, Id);
        if (_mutex == null ||
            Kernel32.GetLastError() == Win32ErrorCode.ERROR_ALREADY_EXISTS ||
            Kernel32.GetLastError() == Win32ErrorCode.ERROR_ACCESS_DENIED)
        {
            DesktopWindow.BroadcastRestore();
            ExitNow();
            return;
        }

        _window = new MainWindow();

        var background = AppInstance.GetActivatedEventArgs() is Activation.IStartupTaskActivatedEventArgs task
            && task?.TaskId == AudioSwitcher.StartupTaskId
            && AudioSwitcher.RunAtStartupMinimized
            && AudioSwitcher.RunInBackground;

        if (background)
            _window.RunInBackround();
        else
            _window.Activate();

        // TODO: Remove workaround once Application.Exit() gets fixed
        // See https://github.com/microsoft/microsoft-ui-xaml/issues/5931
        static void ExitNow()
        {
            Environment.Exit(0);
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IStartupTaskManager, WinStartupTaskManager>();
        services.AddSingleton<IStateStorage, StateStorage>();
        services.AddSingleton<IO, WinIO>();
        services.AddSingleton(x => MainWindow.Instance?.DispatcherQueue ?? throw new ArgumentNullException());
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IHotkeyManager>(x => new HotkeyManager(MainWindow.SharedHwnd));
        services.AddSingleton<IAudioManager, AudioManager>();
        services.AddSingleton<IDispatcher, WinDispatcher>();
        services.AddSingleton<IClipboard, WinClipboard>();
        services.AddSingleton<IApp>(App.Current);
        services.AddSingleton<AudioSwitcher>();
        services.AddSingleton<AudioEvents>();
        services.AddSingleton<Frame>();
        services.AddTransient<AudioDeviceWatcher>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<AudioPageViewModel>();
        return services.BuildServiceProvider();
    }
}
