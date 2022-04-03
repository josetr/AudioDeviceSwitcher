// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<SettingsPageViewModel>();
        InitializeComponent();
    }

    public SettingsPageViewModel ViewModel { get; }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        await ViewModel.LoadAsync();
    }
}
