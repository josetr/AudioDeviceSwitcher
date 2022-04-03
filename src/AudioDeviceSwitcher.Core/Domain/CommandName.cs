// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

public struct CommandName
{
    public CommandName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentOutOfRangeException(null, $"Name cannot be empty.");

        if (name != name.Trim())
            throw new ArgumentOutOfRangeException($"'{name}' cannot contain leading or trailing whitespace.");

        Value = name;
    }

    public string Value { get; }

    public static implicit operator CommandName(string s)
    {
        return new CommandName(s);
    }

    public static implicit operator string(CommandName s)
    {
        return s.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
