﻿namespace Tenekon.Reactive.Events;

/// <summary>
/// Once every <typeparamref name="TWhenever"/> event, the <typeparamref name="TResubscribe"/> event gets subscribed or the existing subscriptions renewed.
/// Then every new <typeparamref name="TResubscribe"/> event and its correlating <typeparamref name="TWhenever"/> event are getting emitted.
/// </summary>
/// <typeparam name="TWhenever"></typeparam>
/// <typeparam name="TResubscribe"></typeparam>
internal class WheneverThenResubscribeEvent<TWhenever, TResubscribe> : Event<(TWhenever, TResubscribe)>
{
    private readonly IObservableEvent<TWhenever> _whenever;
    private readonly IObservableEvent<TResubscribe> _resubscribe;

    public WheneverThenResubscribeEvent(IObservableEvent<TWhenever> whenever, IObservableEvent<TResubscribe> resubscribe)
    {
        _whenever = whenever;
        _resubscribe = resubscribe;
    }

    public override IDisposable Subscribe(IEventObserver<(TWhenever, TResubscribe)> eventObserver) =>
        DelegatingDisposable.Create(_ => {
            var resubscribeDisposables = new DisposableCollection();

            return new DisposableCollection() {
                resubscribeDisposables,
                base.Subscribe(eventObserver),
                _whenever.SubscribeBacklogBacked(
                (wheneverEmissionBacklog, whenever) => {
                    resubscribeDisposables.Dispose(permanently: false);

                    resubscribeDisposables.TryAdd(
                        () => _resubscribe.SubscribeBacklogBacked(
                            wheneverEmissionBacklog,
                            (resubscribeEmissionBacklog, resubscribe) => EvaluateEmission(resubscribeEmissionBacklog, (whenever, resubscribe))),
                        out var _);
                })
            }.Dispose;
        });
}
