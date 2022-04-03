// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public sealed class GroupNotificationService : INotificationService
{
    private readonly INotificationService notificationService;
    private readonly List<string> messages = new();

    public GroupNotificationService(INotificationService notificationService)
    {
        this.notificationService = notificationService;
    }

    public Task ShowNotificationAsync(string message)
    {
        messages.Add(message);
        return Task.CompletedTask;
    }

    public void Commit()
    {
        if (messages.Count > 0)
            notificationService.ShowNotificationAsync(string.Join(Environment.NewLine, messages));
    }
}
