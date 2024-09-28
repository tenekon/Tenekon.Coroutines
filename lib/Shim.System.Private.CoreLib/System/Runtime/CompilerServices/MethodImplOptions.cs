// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    // This Enum matches the miImpl flags defined in corhdr.h. It is used to specify
    // certain method properties.
    public static class MethodImplOptions
    {
        public const short Unmanaged = 0x0004;
        public const short NoInlining = 0x0008;
        public const short ForwardRef = 0x0010;
        public const short Synchronized = 0x0020;
        public const short NoOptimization = 0x0040;
        public const short PreserveSig = 0x0080;
        public const short AggressiveInlining = 0x0100;
        public const short AggressiveOptimization = 0x0200;
        public const short InternalCall = 0x1000;
    }
}