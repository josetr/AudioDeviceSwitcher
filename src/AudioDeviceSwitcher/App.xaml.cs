// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using Microsoft.UI.Xaml;
    using Windows.ApplicationModel;
    using static PInvoke.User32;
    using Activation = Windows.ApplicationModel.Activation;

    public sealed partial class App : Application
    {
        public const string Id = "JoseTorres:AudioDeviceSwitcher";

        private static Lazy<AudioSwitcher> _audioSwitcher = new Lazy<AudioSwitcher>(() => new(new()));
        private static PInvoke.Kernel32.SafeObjectHandle? _mutex;
        private static MainWindow? _window;

        public App()
        {
            InitializeComponent();
        }

        public static AudioSwitcher AudioSwitcher => _audioSwitcher.Value;
        public static MainWindow? Window => _window;
        public static ElementTheme Theme => AudioSwitcher.Settings.DarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        public static void UpdateTheme()
        {
            if (_window?.Content is FrameworkElement e)
                e.RequestedTheme = Theme;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length >= 2)
            {
                try
                {
                    CLI.RunAsync(cmdArgs.Skip(1).ToArray()).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    MessageBox(IntPtr.Zero, e.Message, Settings.Title, MessageBoxOptions.MB_ICONERROR);
                }

                Exit();
                return;
            }

            _mutex = PInvoke.Kernel32.CreateMutex(IntPtr.Zero, false, Id);
            if (_mutex == null ||
                PInvoke.Kernel32.GetLastError() == PInvoke.Win32ErrorCode.ERROR_ALREADY_EXISTS ||
                PInvoke.Kernel32.GetLastError() == PInvoke.Win32ErrorCode.ERROR_ACCESS_DENIED)
            {
                DesktopWindow.BroadcastRestore();
                Exit();
                return;
            }

            _window = new MainWindow(AudioSwitcher);

            var background = AppInstance.GetActivatedEventArgs() is Activation.IStartupTaskActivatedEventArgs task
                && task?.TaskId == "Startup"
                && AudioSwitcher.Settings.RunAtStartupMinimized
                && AudioSwitcher.Settings.RunInBackground;

            if (background)
                _window.RunInBackround();
            else
                _window.Activate();
        }
    }
}