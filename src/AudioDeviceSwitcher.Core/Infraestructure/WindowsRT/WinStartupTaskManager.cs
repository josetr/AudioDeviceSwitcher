// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Windows.ApplicationModel;

public sealed class WinStartupTaskManager : IStartupTaskManager
{
    public async Task<IStartupTask> GetTaskAsync(string id)
    {
        return new WinStartupTask(await StartupTask.GetAsync(id));
    }
}
