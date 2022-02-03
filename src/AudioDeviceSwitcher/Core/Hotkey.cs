// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System;
using System.Collections.Generic;
using Windows.System;

public record Hotkey
{
    public Hotkey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    public Hotkey()
    {
    }

    public static Hotkey Empty { get; } = new();
    public VirtualKeyModifiers Modifiers { get; set; }
    public VirtualKey Key { get; set; }

    public override string ToString()
    {
        var keys = new List<string>(4);

        Span<VirtualKeyModifiers> mods = stackalloc VirtualKeyModifiers[4]
        {
            VirtualKeyModifiers.Control,
            VirtualKeyModifiers.Shift,
            VirtualKeyModifiers.Menu,
            VirtualKeyModifiers.Windows,
        };

        foreach (var mod in mods)
        {
            if (Modifiers.HasFlag(mod))
                keys.Add(GetPrettyModifier(mod));
        }

        if (!Modifiers.HasFlag(VirtualKeyModifiers.Control) &&
            !Modifiers.HasFlag(VirtualKeyModifiers.Menu))
        {
            return string.Empty;
        }

        keys.Add(Key.ToString());
        return string.Join(" + ", keys);
    }

    private static string GetPrettyModifier(VirtualKeyModifiers modifier)
    {
        return modifier switch
        {
            VirtualKeyModifiers.Control => "Ctrl",
            VirtualKeyModifiers.Shift => "Shift",
            VirtualKeyModifiers.Menu => "Alt",
            VirtualKeyModifiers.Windows => "Windows",
            VirtualKeyModifiers.None => "None",
            _ => throw new NotImplementedException(),
        };
    }
}
