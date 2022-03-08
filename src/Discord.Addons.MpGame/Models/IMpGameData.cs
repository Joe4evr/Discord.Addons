using System.Collections.Generic;

namespace Discord.Addons.MpGame;

/// <summary>
/// 
/// </summary>
public interface IMpGameData
{
    /// <summary>
    ///     Determines if a game in the current channel is open to join or not.
    /// </summary>
    bool OpenToJoin { get; }

    /// <summary>
    ///     Id of the public-facing channel of this game.
    /// </summary>
    ulong? GameChannelId { get; }

    /// <summary>
    ///     Id of the player invoking this command.
    /// </summary>
    ulong? PlayerUserId { get; }

    /// <summary>
    ///     Determines if a game in the current channel is in progress or not.
    /// </summary>
    CurrentlyPlaying GameInProgress { get; }

    /// <summary>
    ///     The list of users ready to play.
    /// </summary>
    IReadOnlyCollection<ulong> JoinedUsers { get; }
}
