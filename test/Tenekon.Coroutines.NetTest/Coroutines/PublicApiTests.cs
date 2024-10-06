using PublicApiGenerator;

namespace Tenekon.Coroutines;

public class PublicApiTests
{
#if !TRANSITIVE_COROUTINES_TARGET_FRAMEWORK
    [Fact]
    public Task CoroutinesNetAssembly_HasApprovedPublicApi()
    {
        var assembly = typeof(Coroutine).Assembly;
        var publicApi = assembly.GeneratePublicApi(new() {
            ExcludeAttributes = [
            "System.Reflection.AssemblyMetadataAttribute",
            "System.Runtime.CompilerServices.InternalsVisibleTo"
            ]
        });
        return Verify(publicApi);
    }
#endif
}
