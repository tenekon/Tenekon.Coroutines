﻿namespace Vernuntii.Coroutines;

public static class ValueTaskExtensions
{
    public static Coroutine AsCoroutineInternal(this in ValueTask task) =>
        new Coroutine(in task);

    public static Coroutine AsCoroutine(this in ValueTask task) =>
        task.AsCoroutineInternal();

    public static Coroutine<TResult> AsCoroutineInternal<TResult>(this in ValueTask<TResult> task) =>
        new Coroutine<TResult>(in task);

    public static Coroutine<TResult> AsCoroutine<TResult>(this in ValueTask<TResult> task) =>
        task.AsCoroutineInternal();
}
