// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

public sealed partial class AudioPage : Page, IDisposable
{
    private AudioDeviceClass deviceClass;

    public AudioPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<AudioPageViewModel>();
    }

    public AudioPageViewModel ViewModel { get; }

    public void Dispose()
    {
        ViewModel.Dispose();
    }

    public bool IsHotKeyFocused()
    {
        return Hotkey.FocusState != FocusState.Unfocused;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        deviceClass = (AudioDeviceClass)e.Parameter;
        Loaded += AudioPage_Loaded;
        InitializeComponent();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Dispose();
    }

    private async void AudioPage_Loaded(object sender, RoutedEventArgs e)
    {
        App.Current.UpdateTheme();
        await ViewModel.InitializeAsync(deviceClass, watch: true, AudioListView.SelectedItems);
    }

    private async void HotKey_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        if (AudioDeviceSwitcher.Hotkey.Validate(args.Modifiers.ToKeyModifiers(), args.Key.ToKey()))
            await ViewModel.TrySetHotkeyAsync(new(args.Modifiers.ToKeyModifiers(), args.Key.ToKey()));

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
            await ViewModel.TrySetHotkeyAsync(new());

        Hotkey.SelectionStart = Hotkey.Text.Length;
    }
}
