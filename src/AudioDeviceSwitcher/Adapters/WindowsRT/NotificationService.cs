// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

public sealed class NotificationService : INotificationService
{
    public Task ShowNotificationAsync(string message)
    {
        ToastNotificationManager.History.Clear();

        new ToastContentBuilder()
             .AddText(message)
             .AddAudio(null, silent: true)
             .Show();

        return Task.CompletedTask;
    }

    public static Task ShowErrorNotificationAsync(string message)
    {
        ToastNotificationManager.History.Clear();

        new ToastContentBuilder()
           .AddText($"❌ {message}")
           .Show();

        return Task.CompletedTask;
    }
}
