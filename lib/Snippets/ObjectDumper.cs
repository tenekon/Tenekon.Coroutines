using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Tenekon.Coroutines;

internal static class ObjectDumper
{
    public static void Dump<T>(this T obj)
    {
        Console.WriteLine(JsonConvert.SerializeObject(obj, new JsonSerializerSettings() {
            Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args) {
                args.ErrorContext.Handled = true;
            },
            ContractResolver = new MyContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.All
        }));
    }

    public class MyContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                   .Select(f => base.CreateProperty(f, memberSerialization))
                        .ToList();
            props.ForEach(p => { p.Writable = true; p.Readable = true; });
            return props;
        }
    }
}
