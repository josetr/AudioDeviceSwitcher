// Copyright (c) 2021 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1402 // File may only contain a single type

namespace AudioDeviceSwitcher
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public class ComObject<TInterface> : IDisposable
        where TInterface : class
    {
        private TInterface? _value;

        public ComObject(TInterface obj)
        {
            _value = obj;
        }

        public TInterface Value
        {
            get => _value != null ? _value : throw new NullReferenceException();
            set => _value = value;
        }

        public void Dispose()
        {
            if (_value != null && Marshal.IsComObject(_value))
            {
                Marshal.FinalReleaseComObject(_value);
                _value = null;
            }
        }
    }

    public sealed class ComObject<TInterface, TImp> : ComObject<TInterface>, IDisposable
        where TInterface : class
        where TImp : new()
    {
        public ComObject()
            : base(Unsafe.As<TInterface>(new TImp()))
        {
        }
    }
}