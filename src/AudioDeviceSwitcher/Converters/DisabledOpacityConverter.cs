// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Microsoft.UI.Xaml.Data;

public sealed class DisabledOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value != null && value is bool h && h ? 0.5d : 1.0d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return new NotImplementedException();
    }
}
