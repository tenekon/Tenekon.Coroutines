namespace Tenekon.Reactive.Broker;

internal interface ITypeInversedChainableEventObserver
{
    void OnEmission<T>(EventEmissionBacklog emissionBacklog, T eventData);
}
