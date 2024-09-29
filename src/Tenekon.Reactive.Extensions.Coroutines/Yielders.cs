using System.Text;
using Tenekon.Coroutines;

namespace Tenekon.Reactive.Extensions.Coroutines;

public static partial class Yielders
{
    public partial class Arguments
    {
        public readonly static Key ChannelKey = new(Encoding.ASCII.GetBytes("__co/rx"), 1);
        public readonly static Key TakeKey = new(Encoding.ASCII.GetBytes("__co/rx"), 2);
        public readonly static Key EmitKey = new(Encoding.ASCII.GetBytes("__co/rx"), 3);
    }
}
