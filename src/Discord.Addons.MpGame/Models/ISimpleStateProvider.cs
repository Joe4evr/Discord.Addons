#if NET6_0_OR_GREATER
using System;

namespace Discord.Addons.MpGame
{
    public interface ISimpleStateProvider<TState>
        where TState : struct, Enum
    {
        TState State { get; }
    }
}
#endif
