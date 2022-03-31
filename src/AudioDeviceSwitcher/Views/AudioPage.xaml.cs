// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;

public sealed partial class AudioPage : Page, IDisposable
{
    private AudioSwitcher audioSwitcher;

    public AudioPage()
    {
        audioSwitcher = App.AudioSwitcher;
        ViewModel = new AudioPageViewModel(audioSwitcher)
        {
            DispatcherQueue = DispatcherQueue,
        };
    }

    public AudioPageViewModel ViewModel { get; set; }

    public void Dispose()
    {
        ViewModel.Dispose();
    }

    public bool IsHotKeyFocused()
    {
        return Hotkey.FocusState != FocusState.Unfocused;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        InitializeComponent();
        var @params = (Params)e.Parameter;
        ViewModel.IO = @params.IO;
        ViewModel.DeviceClass = @params.DeviceClass;
        ViewModel.LoadCommands();
        await ViewModel.LoadDevices();

        if (IsLoaded)
            AudioPage_Loaded(null!, null!);
        else
            Loaded += AudioPage_Loaded;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Dispose();
    }

    private void AudioPage_Loaded(object sender, RoutedEventArgs e)
    {
        App.UpdateTheme();
        ViewModel.SwapSelectedDevicesContainer(AudioListView.SelectedItems);
    }

    private async void HotKey_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        await ViewModel.SetHotkeyAsync(new(args.Modifiers, args.Key));
        args.Handled = true;
    }

    private void AudioListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        MenuFlyoutContext.Items.Clear();

        var listView = (ListView)sender;
        var device = (AudioDeviceViewModel?)((FrameworkElement)e.OriginalSource).DataContext;

        foreach (var option in ViewModel.GetDeviceMenuOptions(device))
        {
            if (string.IsNullOrWhiteSpace(option.Name))
            {
                MenuFlyoutContext.Items.Add(new MenuFlyoutSeparator());
                continue;
            }

            var result = new ToggleMenuFlyoutItem() { Text = option.Name, IsChecked = option.IsChecked, IsEnabled = option.IsEnabled };
            result.Click += (_, _) => option.Action();
            MenuFlyoutContext.Items.Add(result);
        }

        if (MenuFlyoutContext.Items.Any())
            MenuFlyoutContext.ShowAt(listView, e.GetPosition(listView));
    }

    private void ShowOptions(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        btn.Flyout.ShowAt(btn);
    }

    private void Hotkey_LostFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveSelectedCommand();
    }

    private async void Hotkey_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Hotkey.Text))
            await ViewModel.SetHotkeyAsync(new());

        Hotkey.SelectionStart = Hotkey.Text.Length;
    }

    public sealed partial class Params
    {
        public Params(DeviceClass deviceClass, IO iO)
        {
            DeviceClass = deviceClass;
            IO = iO;
        }

        public DeviceClass DeviceClass { get; set; }
        public IO IO { get; set; }
    }
}
