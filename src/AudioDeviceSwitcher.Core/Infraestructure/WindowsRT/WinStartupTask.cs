// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Windows.ApplicationModel;

public sealed class WinStartupTask : IStartupTask
{
    private readonly StartupTask task;

    public WinStartupTask(StartupTask task)
    {
        this.task = task;
    }

    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(
            task.State == StartupTaskState.Enabled ||
            task.State == StartupTaskState.EnabledByPolicy);
    }

    public async Task EnableAsync()
    {
        await task.RequestEnableAsync();
    }

    public Task DisableAsync()
    {
        task.Disable();
        return Task.CompletedTask;
    }
}
