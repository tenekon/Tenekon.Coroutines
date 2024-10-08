﻿using Tenekon.Coroutines.Sources;

namespace Tenekon.Coroutines;

internal static class CoroutineAwaiterExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DelegateCompletion(this Task task, ICompletionSource<VoidCoroutineResult> completionSource)
    {
        var taskAwaiter = task.ConfigureAwait(false).GetAwaiter();

        if (task.IsCompleted) {
            CompleteSource(in taskAwaiter, completionSource);
        } else {
            taskAwaiter.UnsafeOnCompleted(() => CompleteSource(in taskAwaiter, completionSource));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CompleteSource(in ConfiguredTaskAwaitable.ConfiguredTaskAwaiter taskAwaiter, ICompletionSource<VoidCoroutineResult> completionSource)
        {
            try {
                taskAwaiter.GetResult();
                completionSource.SetResult(default);
            } catch (Exception error) {
                completionSource.SetException(error);
                //throw; // Must bubble up
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DelegateCompletion<TResult>(this ValueTask<TResult> coroutineAwaiter, ICompletionSource<TResult> completionSource)
    {
        var taskAwaiter = coroutineAwaiter.ConfigureAwait(false).GetAwaiter();

        if (coroutineAwaiter.IsCompleted) {
            CompleteSource(in taskAwaiter, completionSource);
        } else {
            taskAwaiter.UnsafeOnCompleted(() => CompleteSource(in taskAwaiter, completionSource));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CompleteSource(in ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter taskAwaiter, ICompletionSource<TResult> completionSource)
        {
            try {
                var result = taskAwaiter.GetResult();
                completionSource.SetResult(result);
            } catch (Exception error) {
                completionSource.SetException(error);
                //throw; // Must bubble up
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DelegateCoroutineCompletion<TAwaiter>(this ref TAwaiter coroutineAwaiter, ICompletionSource<VoidCoroutineResult> completionSource)
        where TAwaiter : struct, ICriticalNotifyCompletion, ICoroutineAwaiter
    {
        if (coroutineAwaiter.IsCompleted) {
            CompleteSource(in coroutineAwaiter, completionSource);
        } else {
            var coroutineAwaiterCopy = coroutineAwaiter;
            coroutineAwaiter.UnsafeOnCompleted(() => CompleteSource(in coroutineAwaiterCopy, completionSource));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CompleteSource(in TAwaiter coroutineAwaiter, ICompletionSource<VoidCoroutineResult> completionSource)
        {
            try {
                coroutineAwaiter.GetResult();
                completionSource.SetResult(default);
            } catch (Exception error) {
                completionSource.SetException(error);
                //throw; // Must bubble up
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DelegateCoroutineCompletion<TAwaiter, TResult>(this ref TAwaiter coroutineAwaiter, ICompletionSource<TResult> completionSource)
        where TAwaiter : struct, ICriticalNotifyCompletion, ICoroutineAwaiter<TResult>
    {
        if (coroutineAwaiter.IsCompleted) {
            CompleteSource(in coroutineAwaiter, completionSource);
        } else {
            var coroutineAwaiterCopy = coroutineAwaiter;
            coroutineAwaiter.UnsafeOnCompleted(() => CompleteSource(in coroutineAwaiterCopy, completionSource));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CompleteSource(in TAwaiter coroutineAwaiter, ICompletionSource<TResult> completionSource)
        {
            try {
                var result = coroutineAwaiter.GetResult();
                completionSource.SetResult(result);
            } catch (Exception error) {
                completionSource.SetException(error);
                //throw; // Must bubble up
            }
        }
    }
}
