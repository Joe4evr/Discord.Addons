using System;

namespace Discord.Addons.MpGame
{
    /// <summary>
    ///     Contract to tweak behavior of a <see cref="MpGameService{TGame, TPlayer}"/>.
    /// </summary>
    public /*partial*/ interface IMpGameServiceConfig
    {
        //public static IMpGameServiceConfig Default { get; } = new DefaultConfig();

        /// <summary>
        ///     The set of log strings to use.
        /// </summary>
        ILogStrings LogStrings { get; }

        //bool AllowJoinMidGame  { get; }
        //bool AllowLeaveMidGame { get; }
    }
}
