// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Threading.Tasks;

internal sealed class WinIO : IO
{
    private readonly Frame frame;

    public WinIO(Frame frame)
    {
        this.frame = frame;
    }

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
}
