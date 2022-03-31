// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using AudioDeviceSwitcher.Interop;
    using Microsoft.Toolkit.Uwp.Notifications;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Media;
    using Windows.Devices.Enumeration;
    using Windows.System;
    using Windows.UI.Notifications;

    public enum NotifyCommandId
    {
        None,
        Exit,
        Community,
        Settings,
        Open,
        Audio,
    }

    public sealed partial class MainWindow : DesktopWindow, IO
    {
        private const string IconPath = "AudioSwitch.ico";
        private readonly AudioSwitcher audioSwitcher;
        private Frame frame = new Frame();
        private List<string> audioRenderList = new();

        public MainWindow(AudioSwitcher audioSwitcher)
        {
            Title = Settings.Title;
            SetIcon(IconPath);
            SetWindowSize(400, 700);
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBar);
            audioSwitcher.Hwnd = Hwnd;
            NavView.SelectedItem = NavView.MenuItems.FirstOrDefault();
            NavView.Content = frame;
            this.audioSwitcher = audioSwitcher;
            audioSwitcher.IO = this;
        }

        public UIElement TitleBar => CustomTitleBar;

        public async Task ShowMessageAsync(string title, string msg)
        {
            if (VisualTreeHelper.GetOpenPopupsForXamlRoot(frame.XamlRoot).Any(x => x.Child is ContentDialog))
                return;

            var dialog = new ContentDialog()
            {
                Title = title,
                Content = msg,
                CloseButtonText = "OK",
                XamlRoot = frame.XamlRoot,
                RequestedTheme = App.Theme,
            };

            await dialog.ShowAsync();
        }

        public async Task<string?> GetMessageAsync(string title, string defaultValue)
        {
            if (VisualTreeHelper.GetOpenPopupsForXamlRoot(frame.XamlRoot).Any(x => x.Child is ContentDialog))
                return null;

            var input = new TextBox { Height = 32, Text = defaultValue };
            input.SelectionStart = input.Text.Length;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = input,
                PrimaryButtonText = "Ok",
                IsSecondaryButtonEnabled = true,
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = frame.XamlRoot,
                RequestedTheme = App.Theme,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                return input.Text;

            return null;
        }

        public Task ShowNotification(string message)
        {
            ToastNotificationManager.History.Clear();

            new ToastContentBuilder()
                 .AddText(message)
                 .AddAudio(null, silent: true)
                 .Show();

            return Task.CompletedTask;
        }

        public Task ShowErrorNotification(string message)
        {
            ToastNotificationManager.History.Clear();

            new ToastContentBuilder()
               .AddText($"❌ {message}")
               .Show();

            return Task.CompletedTask;
        }

        protected override bool OnClose()
        {
            if (!audioSwitcher.Settings.RunInBackground)
                return true;

            Hide();
            return false;
        }

        protected override async void OnHotkey(int id, VirtualKeyModifiers modifiers, VirtualKey key)
        {
            var audioPage = frame.Content as AudioPage;
            if (audioPage != null && audioPage.IsHotKeyFocused() && IsActive)
            {
                await audioPage.ViewModel.SetHotkeyAsync(new(modifiers, key));
                return;
            }

            try
            {
                await audioSwitcher.OnHotkeyAsync(new Hotkey(modifiers, key));
            }
            catch (Exception e)
            {
                await ShowErrorNotification(e.Message);
            }
        }

        protected override async void OnShellNotifyIconMessage(PInvoke.User32.WindowMessage id)
        {
            switch (id)
            {
                case PInvoke.User32.WindowMessage.WM_LBUTTONDBLCLK:
                    ExecuteCommand(NotifyCommandId.Open);
                    break;
                case PInvoke.User32.WindowMessage.WM_RBUTTONUP:
                    await OpenNotifyMenuTask();
                    break;
            }
        }

        private async Task OpenNotifyMenuTask()
        {
            PInvoke.User32.SetForegroundWindow(Hwnd);
            var menu = User32.CreatePopupMenu();
            var devices = (await DeviceInformation.FindAllAsync(AudioUtil.GetInterfaceGuid(DeviceClass.AudioRender))).ToArray();
            audioRenderList.Clear();

            if (devices.Length > 0)
            {
                foreach (var device in devices)
                {
                    if (AudioUtil.IsDisabled(device.Id))
                        continue;

                    var isDefault = AudioUtil.IsDefault(device.Id, DeviceClass.AudioRender);
                    var flags = PInvoke.User32.MenuItemFlags.MF_STRING;
                    flags |= isDefault ? PInvoke.User32.MenuItemFlags.MF_CHECKED : PInvoke.User32.MenuItemFlags.MF_UNCHECKED;
                    PInvoke.User32.AppendMenu(menu, flags, (IntPtr)NotifyCommandId.Audio + audioRenderList.Count, device.Name);
                    audioRenderList.Add(device.Id);
                }

                PInvoke.User32.AppendMenu(menu, PInvoke.User32.MenuItemFlags.MF_SEPARATOR, default, default);
            }

            PInvoke.User32.AppendMenu(menu, PInvoke.User32.MenuItemFlags.MF_STRING, (IntPtr)NotifyCommandId.Settings, "&Settings");
            PInvoke.User32.AppendMenu(menu, PInvoke.User32.MenuItemFlags.MF_STRING, (IntPtr)NotifyCommandId.Community, "&Community");
            PInvoke.User32.AppendMenu(menu, PInvoke.User32.MenuItemFlags.MF_STRING, (IntPtr)NotifyCommandId.Open, "&Open");
            PInvoke.User32.AppendMenu(menu, PInvoke.User32.MenuItemFlags.MF_STRING, (IntPtr)NotifyCommandId.Exit, "&Exit");
            PInvoke.User32.GetCursorPos(out var point);
            ExecuteCommand((NotifyCommandId)User32.TrackPopupMenuEx(menu, User32.TPM_RIGHTALIGN | User32.TPM_RETURNCMD, point.x, point.y, Hwnd, default));
            User32.DestroyMenu(menu);
        }

        private async void ExecuteCommand(NotifyCommandId command)
        {
            switch (command)
            {
                case NotifyCommandId.Open:
                    Restore();
                    break;
                case NotifyCommandId.Settings:
                    NavView.SelectedItem = NavView.SettingsItem;
                    Restore();
                    break;
                case NotifyCommandId.Community:
                    await Launcher.LaunchUriAsync(new(Settings.Discord));
                    break;
                case NotifyCommandId.Exit:
                    Close();
                    return;
            }

            if (command >= NotifyCommandId.Audio)
            {
                var index = command - NotifyCommandId.Audio;
                await audioSwitcher.ToggleAsync(DeviceClass.AudioRender, new[] { audioRenderList[index] }, audioSwitcher.Settings.SwitchCommunicationDevice, null, _ => Task.CompletedTask);
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Content is string content)
            {
                switch (content)
                {
                    case "Playback":
                        frame.Navigate(typeof(AudioPage), new AudioPage.Params(DeviceClass.AudioRender, this));
                        break;
                    case "Recording":
                        frame.Navigate(typeof(AudioPage), new AudioPage.Params(DeviceClass.AudioCapture, this));
                        break;
                    case "About":
                        frame.Navigate(typeof(AboutPage));
                        break;
                }
            }
            else if (args.IsSettingsSelected)
            {
                frame.Navigate(typeof(SettingsPage), this);
            }
        }
    }
}