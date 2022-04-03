// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Devices.Enumeration;

public sealed class DeviceThumbnailImgConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || value is not DeviceThumbnail thumbnail)
            return null;

        var img = new BitmapImage();
        img.SetSource(thumbnail);
        return img;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return new NotImplementedException();
    }
}
