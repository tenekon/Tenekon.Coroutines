namespace Tenekon.Coroutines;

internal class CoroutineContextEqualityComparer : EqualityComparer<CoroutineContext>
{
    public static bool Equals(in CoroutineContext x, in CoroutineContext y)
    {
        return ReferenceEquals(x.ResultStateMachine, y.ResultStateMachine)
            && ReferenceEquals(x.KeyedServices, y.KeyedServices)
            && ReferenceEquals(x.KeyedServicesToBequest, y.KeyedServicesToBequest)
            && x._bequesterOrigin == y._bequesterOrigin
            && ReferenceEquals(x._bequestContext, y._bequestContext)
            && x._isCoroutineAsyncIteratorSupplier == y._isCoroutineAsyncIteratorSupplier
#if DEBUG
            && x._identifier == y._identifier
#endif
            ;
    }

    public static int GetHashCode([DisallowNull] in CoroutineContext obj)
    {
        var hash = new HashCode();
        hash.Add(obj.ResultStateMachine);
        hash.Add(obj.KeyedServices);
        hash.Add(obj.KeyedServicesToBequest);
        hash.Add(obj._bequesterOrigin);
        hash.Add(obj._bequestContext);
        hash.Add(obj._isCoroutineAsyncIteratorSupplier);
#if DEBUG
        hash.Add(obj._identifier);
#endif
        return hash.ToHashCode();
    }

    public new static readonly CoroutineContextEqualityComparer Default = new();

    public override bool Equals(CoroutineContext x, CoroutineContext y) => Equals(in x, in y);
    public override int GetHashCode([DisallowNull] CoroutineContext obj) => obj.GetHashCode();
}
