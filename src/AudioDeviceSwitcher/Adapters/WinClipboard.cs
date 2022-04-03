// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace AudioDeviceSwitcher;

using Windows.ApplicationModel.DataTransfer;

public sealed class WinClipboard : IClipboard
{
    public void SetTextContent(string text)
    {
        var content = new DataPackage();
        content.SetText(text);
        Clipboard.SetContent(content);
    }
}
