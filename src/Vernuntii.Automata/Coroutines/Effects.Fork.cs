﻿using System.Runtime.CompilerServices;
using System.Text;

namespace Vernuntii.Coroutines;

partial class Effects
{
    internal readonly static ArgumentType ForkArgumentType = new ArgumentType(Encoding.ASCII.GetBytes("@vernuntii"), Encoding.ASCII.GetBytes("fork"));

    public async static Coroutine<Coroutine> ForkAsync(Func<Coroutine> provider)
    {
        var completionSource = Coroutine<Coroutine>.CompletionSource.RentFromCache();
        return await new Coroutine<Coroutine>(completionSource.CreateValueTask(), ArgumentReceiverDelegate);

        void ArgumentReceiverDelegate(ref CoroutineArgumentReceiver argumentReceiver)
        {
            var argument = new ForkArgument(provider, completionSource);
            argumentReceiver.ReceiveArgument(in argument, in ForkArgumentType);
        }
    }

    public async static Coroutine<Coroutine<T>> ForkAsync<T>(Func<Coroutine<T>> provider)
    {
        var completionSource = Coroutine<Coroutine<T>>.CompletionSource.RentFromCache();
        return await new Coroutine<Coroutine<T>>(completionSource.CreateValueTask(), ArgumentReceiverDelegate);

        void ArgumentReceiverDelegate(ref CoroutineArgumentReceiver argumentReceiver)
        {
            var argument = new ForkArgument<T>(provider, completionSource);
            argumentReceiver.ReceiveArgument(in argument, ForkArgumentType);
        }
    }

    internal ref struct ForkCoroutineAwaiterReceiver(ref CoroutineStackNode coroutineNode)
    {
        private ref CoroutineStackNode _coroutineNode = ref coroutineNode;

        public void ReceiveCoroutineAwaiter<T>(ref T awaiter) where T : ICriticalNotifyCompletion
        {
            ref var coroutineAwaiter = ref Unsafe.As<T, Coroutine.CoroutineAwaiter>(ref awaiter);
            coroutineAwaiter.PropagateCoroutineNode(ref _coroutineNode);
            coroutineAwaiter.StartStateMachine();
        }
    }

    internal struct ForkArgument(Func<Coroutine> provider, Coroutine<Coroutine>.CompletionSource completionSource)
    {
        private readonly Func<Coroutine> _provider = provider;

        public void CreateCoroutine(ref ForkCoroutineAwaiterReceiver awaiterReceiver)
        {
            var coroutine = _provider();
            var awaiter = coroutine.GetAwaiter();
            awaiterReceiver.ReceiveCoroutineAwaiter(ref awaiter);
            completionSource.SetResult(coroutine);
        }
    }

    internal struct ForkArgument<T>(Func<Coroutine<T>> provider, Coroutine<Coroutine<T>>.CompletionSource completionSource)
    {
        private readonly Func<Coroutine<T>> _provider = provider;

        public void CreateCoroutine(ref ForkCoroutineAwaiterReceiver awaiterReceiver)
        {
            var coroutine = _provider();
            var awaiter = coroutine.GetAwaiter();
            awaiterReceiver.ReceiveCoroutineAwaiter(ref awaiter);
            completionSource.SetResult(coroutine);
        }
    }
}
