﻿namespace Tenekon.Coroutines;

internal interface IValueTaskCompletionSource<TResult>
{
    void SetResult(TResult result);
    void SetException(Exception e);
}
