// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using System;
using System.Collections.ObjectModel;
using System.Linq;

public static class Extensions
{
    public static void Remove<T>(this Collection<T> collection, Func<T, bool> condition)
    {
        var items = collection.Where(condition).ToArray();
        foreach (var item in items)
            collection.Remove(item);
    }
}
