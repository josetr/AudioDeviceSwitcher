// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System.Text.Json;

public sealed class Settings
{
    public static string Repository { get; } = "https://github.com/josetr/AudioDeviceSwitcher";
    public static string Discord { get; } = "https://discord.gg/RZtgA6P4XP";
    public static string Title { get; } = "Audio Device Switcher";

    public Command[] Commands { get; set; } = Array.Empty<Command>();
    public bool RunAtStartup { get; set; } = true;
    public bool RunAtStartupMinimized { get; set; } = true;
    public bool RunInBackground { get; set; } = true;
    public bool ShowDisabledDevices { get; set; } = false;
    public bool SwitchCommunicationDevice { get; set; } = false;
    public bool DarkTheme { get; set; } = true;

    public static Settings Load()
    {
        if (!IsWindowsStorageAvailable())
            return new();

        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        var content = localSettings.Values["settings"]?.ToString();
        return string.IsNullOrWhiteSpace(content) ? new() : (JsonSerializer.Deserialize<Settings>(content) ?? new());
    }

    public void Save()
    {
        if (!IsWindowsStorageAvailable())
            return;

        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        localSettings.Values["settings"] = JsonSerializer.Serialize(this);
    }

    private static bool IsWindowsStorageAvailable()
    {
        try
        {
            return Windows.Storage.ApplicationData.Current != null;
        }
        catch
        {
        }

        return false;
    }
}
