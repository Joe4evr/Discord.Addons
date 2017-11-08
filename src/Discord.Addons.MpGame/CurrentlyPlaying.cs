using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    /// <summary> Specifies if a game is being played when the command is invoked. </summary>
    public enum CurrentlyPlaying
    {
        /// <summary> No game is being played in this channel. </summary>
        None = 0,

        /// <summary> This game is being played in this channel. </summary>
        ThisGame = 1,

        /// <summary> A different game is being played in this channel. </summary>
        DifferentGame = 2
    }
}
