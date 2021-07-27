using System;

namespace Discord.Addons.MpGame
{
    /// <summary>
    ///     Provides the associated game's current state for other components.
    /// </summary>
    /// <typeparam name="TState">
    ///     The state type. This must be an <see langword="enum"/> type.
    /// </typeparam>
    public interface ISimpleStateProvider<TState>
        where TState : struct, Enum
    {
        /// <summary>
        ///     The current state.
        /// </summary>
        TState State { get; }
    }
}
