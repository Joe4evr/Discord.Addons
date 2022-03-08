using System;

namespace Discord.Addons.MpGame;

/// <summary>
///     Contract to tweak behavior of a <see cref="MpGameService{TGame, TPlayer}"/>.
/// </summary>
public partial interface IMpGameServiceConfig
{
    /// <summary>
    ///     The set of log strings to use.
    /// </summary>
    ILogStrings LogStrings { get; }

    /// <summary>
    ///     Indicates if it's allowed for new players to join in the middle of a game.
    /// </summary>
    bool AllowJoinMidGame { get; }

    /// <summary>
    ///     Indicates if it's allowed for players to leave in the middle of a game.
    /// </summary>
    bool AllowLeaveMidGame { get; }
}
