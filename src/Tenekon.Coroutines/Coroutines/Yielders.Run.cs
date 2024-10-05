using Tenekon.Coroutines.CompilerServices;
using Tenekon.Coroutines.Sources;

namespace Tenekon.Coroutines;

partial class Yielders
{
    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct RunArgumentCore<TClosure, TResult>(
            Delegate provider,
            TClosure closure,
            CoroutineProviderFlags providerFlags,
            CancellationToken cancellationToken,
            ManualResetCoroutineCompletionSource<TResult>? completionSource)
        {
            internal readonly ManualResetCoroutineCompletionSource<TResult>? _completionSource = completionSource;

            public readonly Delegate Provider = provider;
            public readonly TClosure Closure = closure;
            public readonly CoroutineProviderFlags ProviderFlags = providerFlags;
            public readonly CancellationToken CancellationToken = cancellationToken;

            public bool Equals(in RunArgumentCore<TClosure, TResult> other) =>
                ReferenceEquals(Provider, other.Provider)
                && Equals(Closure, other.Closure)
                && ProviderFlags == other.ProviderFlags
                && CancellationToken == other.CancellationToken;

            public override bool Equals([AllowNull] object obj) => throw new NotImplementedException();

            public override int GetHashCode() => HashCode.Combine(Provider, Closure, ProviderFlags, CancellationToken);
        }

        internal abstract class AbstractRunArgumentState<TArgument, TCoroutineAwaiter>(TArgument argument, ICoroutineResultStateMachineHolder resultStateMachine)
            where TCoroutineAwaiter : struct
        {
            public readonly TArgument Argument = argument;
            public readonly ICoroutineResultStateMachineHolder ResultStateMachine = resultStateMachine;
            public TCoroutineAwaiter CoroutineAwaiter;
            public CoroutineContext ContextToBequest;
        }

        public class RunArgument<TClosure> : ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable>>, ISiblingCoroutine
        {
            internal readonly RunArgumentCore<TClosure, CoroutineAwaitable> _core;

            public Delegate Provider {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Provider;
            }

            public TClosure Closure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Closure;
            }

            public CoroutineProviderFlags ProviderFlags {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.ProviderFlags;
            }

            public CancellationToken CancellationToken {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.CancellationToken;
            }

            internal RunArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, CancellationToken cancellationToken, ManualResetCoroutineCompletionSource<CoroutineAwaitable> completionSource) =>
                _core = new RunArgumentCore<TClosure, CoroutineAwaitable>(provider, closure, providerFlags, cancellationToken, completionSource);

            public RunArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, CancellationToken cancellationToken) =>
                _core = new RunArgumentCore<TClosure, CoroutineAwaitable>(provider, closure, providerFlags, cancellationToken, completionSource: null);

            public RunArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new RunArgumentCore<TClosure, CoroutineAwaitable>(provider, closure, providerFlags, CancellationToken.None, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<CoroutineAwaitable> completionSource)
            {
                var state = new RunArgumentState(
                    argument: this,
                    context.ResultStateMachine);
                Coroutine coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine>>(_core.Provider)(_core.Closure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine>>(_core.Provider)();
                }
                state.CoroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                state.ContextToBequest.TreatAsNewSibling();
                CoroutineContext.InheritOrBequestCoroutineContext(ref state.ContextToBequest, context);

                state.ResultStateMachine.IncrementBackgroundTasks();
                var taskProxy = Task.Run(state.ActOnCoroutine);
                taskProxy.ContinueWith(state.ContinueAfterActingCoroutine);
                coroutine._task = new(taskProxy);
                coroutine.MarkCoroutineAsActedOn();
                completionSource.SetResult(new(coroutine));
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in RunKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is RunArgument<TClosure> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();

            internal class RunArgumentState(RunArgument<TClosure> argument, ICoroutineResultStateMachineHolder resultStateMachine)
                : AbstractRunArgumentState<RunArgument<TClosure>, ConfiguredCoroutineAwaiter>(argument, resultStateMachine)
            {
                internal Task ActOnCoroutine()
                {
                    var completionSource = new TaskCompletionSourceWrapper<VoidCoroutineResult>();
                    CoroutineMethodBuilderCore.ActOnCoroutine(ref CoroutineAwaiter, in ContextToBequest);
                    CoroutineAwaiter.DelegateCoroutineCompletion(completionSource);
                    return completionSource.Task;
                }

                internal void ContinueAfterActingCoroutine(Task _) => ResultStateMachine.DecrementBackgroundTasks();
            }
        }

        public class RunArgument<TClosure, TResult> : ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>>, ISiblingCoroutine
        {
            internal readonly RunArgumentCore<TClosure, CoroutineAwaitable<TResult>> _core;

            public Delegate Provider {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Provider;
            }

            public TClosure Closure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Closure;
            }

            public CoroutineProviderFlags ProviderFlags {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.ProviderFlags;
            }

            public CancellationToken CancellationToken {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.CancellationToken;
            }

            internal RunArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, CancellationToken cancellationToken, ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>> completionSource) =>
                _core = new RunArgumentCore<TClosure, CoroutineAwaitable<TResult>>(provider, closure, providerFlags, cancellationToken, completionSource);

            public RunArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, CancellationToken cancellationToken) =>
                _core = new RunArgumentCore<TClosure, CoroutineAwaitable<TResult>>(provider, closure, providerFlags, cancellationToken, completionSource: null);

            public RunArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new RunArgumentCore<TClosure, CoroutineAwaitable<TResult>>(provider, closure, providerFlags, CancellationToken.None, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>> completionSource)
            {
                var state = new RunArgumentState(
                    argument: this,
                    context.ResultStateMachine);
                Coroutine<TResult> coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine<TResult>>>(_core.Provider)(_core.Closure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine<TResult>>>(_core.Provider)();
                }
                state.CoroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                state.ContextToBequest.TreatAsNewSibling();
                CoroutineContext.InheritOrBequestCoroutineContext(ref state.ContextToBequest, context);

                state.ResultStateMachine.IncrementBackgroundTasks();
                var taskProxy = Task.Run(state.ActOnCoroutine);
                taskProxy.ContinueWith(state.ContinueAfterActingCoroutine);
                coroutine._task = new(taskProxy);
                coroutine.MarkCoroutineAsActedOn();
                completionSource.SetResult(new(coroutine));
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in RunKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is RunArgument<TClosure, TResult> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();

            internal class RunArgumentState(RunArgument<TClosure, TResult> argument, ICoroutineResultStateMachineHolder resultStateMachine)
                : AbstractRunArgumentState<RunArgument<TClosure, TResult>, ConfiguredCoroutineAwaiter<TResult>>(argument, resultStateMachine)
            {
                internal Task<TResult> ActOnCoroutine()
                {
                    var completionSource = new TaskCompletionSourceWrapper<TResult>();
                    CoroutineMethodBuilderCore.ActOnCoroutine(ref CoroutineAwaiter, in ContextToBequest);
                    CoroutineAwaiter.DelegateCoroutineCompletion(completionSource);
                    return completionSource.Task;
                }

                internal void ContinueAfterActingCoroutine(Task<TResult> _) => ResultStateMachine.DecrementBackgroundTasks();
            }
        }
    }
}
