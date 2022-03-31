// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public sealed class NullIO : IO
{
    public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;
    public Task<string?> GetMessageAsync(string message, string defaultValue) => Task.FromResult<string?>(string.Empty);
    public Task ShowNotification(string message) => Task.CompletedTask;
}
