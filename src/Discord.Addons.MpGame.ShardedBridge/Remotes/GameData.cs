using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using BasePlaying = Discord.Addons.MpGame.CurrentlyPlaying;

namespace Discord.Addons.MpGame.Remotes
{
    public sealed partial class GameData : IMpGameData
    {
        ulong? IMpGameData.GameChannelId => (GameChannelId == 0ul) ? null : (ulong?)GameChannelId;
        ulong? IMpGameData.PlayerUserId => (PlayerUserId == 0ul) ? null : (ulong?)PlayerUserId;
        BasePlaying IMpGameData.GameInProgress => (BasePlaying)GameInProgress;
        IReadOnlyCollection<ulong> IMpGameData.JoinedUsers => JoinedUsers;

        [return: NotNullIfNotNull("other")]
        public static GameData? CopyFrom(IMpGameData? other)
        {
            if (other is null) return null;
            if (other is GameData data) return data;

            data = new GameData
            {
                OpenToJoin = other.OpenToJoin,
                GameChannelId = other.GameChannelId ?? 0ul,
                PlayerUserId = other.PlayerUserId ?? 0ul,
                GameInProgress = (CurrentlyPlaying)other.GameInProgress
            };
            data.JoinedUsers.AddRange(other.JoinedUsers);
            return data;
        }
    }
}
