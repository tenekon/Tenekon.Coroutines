﻿namespace Tenekon.Coroutines;

internal static class GlobalRuntimeFeature
{
    public static readonly bool IsDynamicCodeSupported = RuntimeFeature.IsDynamicCodeSupported;
}
