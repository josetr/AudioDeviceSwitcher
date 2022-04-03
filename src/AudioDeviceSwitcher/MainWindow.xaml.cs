// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;

public sealed partial class MainWindow : DesktopWindow
{
    private readonly Frame frame;
    private readonly AudioSwitcher audioSwitcher;
    private readonly IAudioManager audioManager;

    public MainWindow()
        : base(AudioSwitcherState.Title, "AudioSwitch.ico")
    {
        var services = App.Current.Services;
        Instance = this;
        frame = services.GetRequiredService<Frame>();
        audioManager = services.GetRequiredService<IAudioManager>();
        App.AudioSwitcher = audioSwitcher = services.GetRequiredService<AudioSwitcher>();
        SetWindowSize(400, 700);
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        NavView.SelectedItem = NavView.MenuItems.First();
        NavView.Content = frame;
    }

    public UIElement TitleBar => CustomTitleBar;
    internal static MainWindow? Instance { get; private set; }

    public void RunInBackround()
    {
        Hide();
        ShellIcon.Create();
    }

    protected override bool OnClose()
    {
        if (!audioSwitcher.RunInBackground)
            return true;

        Hide();
        return false;
    }

    protected override async void OnHotkey(int id, KeyModifiers modifiers, Key key)
    {
        var audioPage = frame.Content as AudioPage;

        if (audioPage != null && audioPage.IsHotKeyFocused() && IsActive)
        {
            await audioPage.ViewModel.TrySetHotkeyAsync(new(modifiers, key));
            return;
        }

        try
        {
            await audioSwitcher.ExecuteCommandByHotkeyAsync(new Hotkey(modifiers, key));
        }
        catch (Exception e)
        {
            await NotificationService.ShowErrorNotificationAsync(e.Message);
        }
    }

    protected override async void OnShellNotifyIconMessage(PInvoke.User32.WindowMessage id)
    {
        switch (id)
        {
            case PInvoke.User32.WindowMessage.WM_LBUTTONDBLCLK:
                Restore();
                break;
            case PInvoke.User32.WindowMessage.WM_RBUTTONUP:
                await OpenNotifyMenuAsync();
                break;
        }
    }

    private async Task OpenNotifyMenuAsync()
    {
        var devices = await audioManager.GetAllDevicesAsync(AudioDeviceClass.Render);
        var menu = new WinMenu(Hwnd);

        foreach (var device in devices.Where(x => !audioManager.IsDisabled(x.Id)))
        {
            var isDefault = audioManager.IsDefault(device.Id, AudioDeviceClass.Render, AudioDeviceRoleType.Default);
            var isDisabled = !audioManager.IsActive(device.Id);
            async void Callback()
            {
                await audioSwitcher.ToggleAsync(AudioDeviceClass.Render, new[] { device.Id }, new NoNotificationService());
            }

            menu.Add(new(device.Name, Callback, isDefault, isDisabled));
        }

        if (!menu.IsEmpty)
            menu.Add(null);
        menu.Add(new("&Settings", () =>
        {
            Restore();
            NavView.SelectedItem = NavView.SettingsItem;
        }));
        menu.Add(new("&Community", async () => await Launcher.LaunchUriAsync(new(AudioSwitcherState.Discord))));
        menu.Add(new("&Open", () => Restore()));
        menu.Add(new("&Exit", () => Close()));
        menu.Track();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Content is string content)
        {
            switch (content)
            {
                case "Playback":
                    frame.Navigate(typeof(AudioPage), AudioDeviceClass.Render);
                    break;
                case "Recording":
                    frame.Navigate(typeof(AudioPage), AudioDeviceClass.Capture);
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
