﻿namespace Tenekon.Coroutines;

internal interface IRelativeCoroutine
{
    object? CoroutineActioner { get; }
    CoroutineAction CoroutineAction { get; }

    void MarkCoroutineAsActedOn();
}
