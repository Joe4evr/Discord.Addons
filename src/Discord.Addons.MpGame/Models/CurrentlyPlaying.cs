using System;

namespace Discord.Addons.MpGame;

/// <summary>
///     Specifies if a game is being played when the command is invoked.
/// </summary>
public enum CurrentlyPlaying : byte
{
    /// <summary>
    ///     No game is being played in this channel.
    /// </summary>
    None = 0,

    /// <summary>
    ///     This game is being played in this channel.
    /// </summary>
    ThisGame = 1,

    /// <summary>
    ///     A different game is being played in this channel.
    /// </summary>
    DifferentGame = 2
}
