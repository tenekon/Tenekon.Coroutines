using Nito.Comparers;

namespace Tenekon.Coroutines.GeneratedSources;

internal static class TenekonEqualityComparerBuilder
{
    public static EqualityComparerBuilderFor<T> ForElementsOf<T>(IncrementalValuesProvider<T> provider) => EqualityComparerBuilderFor<T>.Instance;
}
