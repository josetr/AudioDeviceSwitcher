// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher
{
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Navigation;

    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        public SettingsPageViewModel? ViewModel { get; set; }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel = new(App.AudioSwitcher) { IO = (IO)e.Parameter };
            await ViewModel.Load();
        }
    }
}