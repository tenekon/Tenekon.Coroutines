using Tenekon.Coroutines.CompilerServices;
using Tenekon.Coroutines.Sources;

namespace Tenekon.Coroutines;

partial class Yielders
{
    partial class Arguments
    {
        internal enum TaskFactoryExecutionPath
        {
            None,
            CancellationToken,
            CreationOptions,
            Scheduler
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct StartNewArgumentCore<TClosure, TResult>(
            Delegate provider,
            TClosure closure,
            CoroutineProviderFlags providerFlags,
            CancellationToken cancellationToken,
            TaskCreationOptions creationOptions,
            TaskScheduler? scheduler,
            TaskFactoryExecutionPath taskFactoryExecutionPath,
            ManualResetCoroutineCompletionSource<TResult>? completionSource)
        {
            internal readonly ManualResetCoroutineCompletionSource<TResult>? _completionSource = completionSource;
            internal readonly TaskFactoryExecutionPath _taskFactoryExecutionPath = taskFactoryExecutionPath;

            public readonly Delegate Provider = provider;
            public readonly TClosure Closure = closure;
            public readonly CoroutineProviderFlags ProviderFlags = providerFlags;
            public readonly CancellationToken CancellationToken = cancellationToken;
            public readonly TaskCreationOptions CreationOptions = creationOptions;
            public readonly TaskScheduler? Scheduler = scheduler;

            public bool Equals(in StartNewArgumentCore<TClosure, TResult> other) =>
                ReferenceEquals(Provider, other.Provider)
                && Equals(Closure, other.Closure)
                && ProviderFlags == other.ProviderFlags
                && CancellationToken == other.CancellationToken;

            public override bool Equals([AllowNull] object obj) => throw new NotImplementedException();

            public override int GetHashCode() => HashCode.Combine(Provider, Closure, ProviderFlags, CancellationToken, CreationOptions, Scheduler);
        }

        private class AbstractStartNewArgumentState<TArgument, TCoroutineAwaiter>(
            TArgument argument,
            ICoroutineResultStateMachineHolder resultStateMachine)
            where TCoroutineAwaiter : struct
        {
            public readonly TArgument Argument = argument;
            public readonly ICoroutineResultStateMachineHolder ResultStateMachine = resultStateMachine;
            public TCoroutineAwaiter CoroutineAwaiter;
            public CoroutineContext ContextToBequest;
        }

        public class StartNewArgument<TClosure> : ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable>>, ISiblingCoroutine
        {
            internal readonly StartNewArgumentCore<TClosure, CoroutineAwaitable> _core;

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

            public TaskCreationOptions CreationOptions {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.CreationOptions;
            }

            public TaskScheduler? Scheduler {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Scheduler;
            }

            internal StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags,
                CancellationToken cancellationToken,
                TaskCreationOptions creationOptions,
                TaskScheduler? scheduler,
                TaskFactoryExecutionPath taskFactoryExecutionPath,
                ManualResetCoroutineCompletionSource<CoroutineAwaitable> completionSource) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable>(
                    provider,
                    closure,
                    providerFlags,
                    cancellationToken,
                    creationOptions,
                    scheduler,
                    taskFactoryExecutionPath,
                    completionSource);

            public StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags,
                CancellationToken cancellationToken,
                TaskCreationOptions creationOptions,
                TaskScheduler scheduler) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable>(
                    provider,
                    closure,
                    providerFlags,
                    cancellationToken,
                    creationOptions,
                    scheduler,
                    TaskFactoryExecutionPath.Scheduler,
                    completionSource: null);

            public StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags,
                CancellationToken cancellationToken) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable>(
                    provider,
                    closure,
                    providerFlags,
                    cancellationToken,
                    TaskCreationOptions.None,
                    scheduler: null,
                    TaskFactoryExecutionPath.CancellationToken,
                    completionSource: null);

            public StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags,
                TaskCreationOptions creationOptions) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable>(
                    provider,
                    closure,
                    providerFlags,
                    CancellationToken.None,
                    creationOptions,
                    scheduler: null,
                    TaskFactoryExecutionPath.CreationOptions,
                    completionSource: null);

            public StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable>(
                    provider,
                    closure,
                    providerFlags,
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    scheduler: null,
                    TaskFactoryExecutionPath.CreationOptions,
                    completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<CoroutineAwaitable> completionSource)
            {
                var state = new StartNewArgumentState(
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
                var taskProxy = (_core._taskFactoryExecutionPath switch {
                    TaskFactoryExecutionPath.Scheduler => Task<Task>.Factory.StartNew(state.ActOnCoroutine, _core.CancellationToken, _core.CreationOptions, _core.Scheduler!),
                    TaskFactoryExecutionPath.CancellationToken => Task<Task>.Factory.StartNew(state.ActOnCoroutine, _core.CancellationToken),
                    TaskFactoryExecutionPath.CreationOptions => Task<Task>.Factory.StartNew(state.ActOnCoroutine, _core.CreationOptions),
                    _ => Task<Task>.Factory.StartNew(state.ActOnCoroutine)
                }).Unwrap();
                taskProxy.ContinueWith(state.ContinueAfterActingCoroutine);
                coroutine._task = new(taskProxy);
                coroutine.MarkCoroutineAsActedOn();
                completionSource.SetResult(new(coroutine));
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in StartNewKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is StartNewArgument<TClosure> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();

            private class StartNewArgumentState(StartNewArgument<TClosure> argument, ICoroutineResultStateMachineHolder resultStateMachine)
                : AbstractStartNewArgumentState<StartNewArgument<TClosure>, ConfiguredCoroutineAwaiter>(argument, resultStateMachine)
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

        public class StartNewArgument<TClosure, TResult> : ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>>, ISiblingCoroutine
        {
            internal readonly StartNewArgumentCore<TClosure, CoroutineAwaitable<TResult>> _core;

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

            public TaskCreationOptions CreationOptions {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.CreationOptions;
            }

            public TaskScheduler? Scheduler {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Scheduler;
            }

            internal StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags,
                CancellationToken cancellationToken,
                TaskCreationOptions creationOptions,
                TaskScheduler? scheduler,
                TaskFactoryExecutionPath taskFactoryExecutionPath,
                ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>> completionSource) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable<TResult>>(
                    provider,
                    closure,
                    providerFlags,
                    cancellationToken,
                    creationOptions,
                    scheduler,
                    taskFactoryExecutionPath,
                    completionSource);

            public StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags,
                CancellationToken cancellationToken,
                TaskCreationOptions creationOptions,
                TaskScheduler scheduler) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable<TResult>>(
                    provider,
                    closure,
                    providerFlags,
                    cancellationToken,
                    creationOptions,
                    scheduler,
                    TaskFactoryExecutionPath.Scheduler,
                    completionSource: null);

            public StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags,
                CancellationToken cancellationToken) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable<TResult>>(
                    provider,
                    closure,
                    providerFlags,
                    cancellationToken,
                    TaskCreationOptions.None,
                    scheduler: null,
                    TaskFactoryExecutionPath.CancellationToken,
                    completionSource: null);

            public StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags,
                TaskCreationOptions creationOptions) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable<TResult>>(
                    provider,
                    closure,
                    providerFlags,
                    CancellationToken.None,
                    creationOptions,
                    scheduler: null,
                    TaskFactoryExecutionPath.CreationOptions,
                    completionSource: null);

            public StartNewArgument(
                Delegate provider,
                TClosure closure,
                CoroutineProviderFlags providerFlags) =>
                _core = new StartNewArgumentCore<TClosure, CoroutineAwaitable<TResult>>(
                    provider,
                    closure,
                    providerFlags,
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    scheduler: null,
                    TaskFactoryExecutionPath.CreationOptions,
                    completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>> completionSource)
            {
                var state = new StartNewArgumentState(argument: this, context.ResultStateMachine);
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
                var taskProxy = (_core._taskFactoryExecutionPath switch {
                    TaskFactoryExecutionPath.Scheduler => Task<Task<TResult>>.Factory.StartNew(state.ActOnCoroutine, _core.CancellationToken, _core.CreationOptions, _core.Scheduler!),
                    TaskFactoryExecutionPath.CancellationToken => Task<Task<TResult>>.Factory.StartNew(state.ActOnCoroutine, _core.CancellationToken),
                    TaskFactoryExecutionPath.CreationOptions => Task<Task<TResult>>.Factory.StartNew(state.ActOnCoroutine, _core.CreationOptions),
                    _ => Task<Task<TResult>>.Factory.StartNew(state.ActOnCoroutine)
                }).Unwrap();
                taskProxy.ContinueWith(state.ContinueAfterActingCoroutine);
                coroutine._task = new(taskProxy);
                coroutine.MarkCoroutineAsActedOn();
                completionSource.SetResult(new(coroutine));
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in StartNewKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is StartNewArgument<TClosure, TResult> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();

            private class StartNewArgumentState(StartNewArgument<TClosure, TResult> argument, ICoroutineResultStateMachineHolder resultStateMachine)
                : AbstractStartNewArgumentState<StartNewArgument<TClosure, TResult>, ConfiguredCoroutineAwaiter<TResult>>(argument, resultStateMachine)
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
