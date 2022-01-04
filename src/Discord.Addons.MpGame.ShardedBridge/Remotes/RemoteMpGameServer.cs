using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.MpGame.ShardedBridge;
using Grpc.Core;

namespace Discord.Addons.MpGame.Remotes
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TGame">
    ///     The type of game that is managed.
    /// </typeparam>
    /// <typeparam name="TPlayer">
    ///     The type of the <see cref="Player"/> object.
    /// </typeparam>
    public abstract class RemoteMpGameServer<TGame, TPlayer> : RemoteMpGameService.RemoteMpGameServiceBase
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        public abstract override Task<GameData> GetGameData(
            CommandContext request, ServerCallContext context);
    }
}
