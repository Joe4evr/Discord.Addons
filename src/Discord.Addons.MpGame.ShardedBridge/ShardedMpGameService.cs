using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Grpc.Core;
using Discord.WebSocket;
using Discord.Addons.MpGame.Remotes;

namespace Discord.Addons.MpGame.ShardedBridge
{
    using RemoteClient = RemoteMpGameService.RemoteMpGameServiceClient;

    /// <summary>
    ///     Service managing games for a <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/>
    ///     in a multi-process sharded way using gRPC to communicate with the remote process.
    /// </summary>
    /// <typeparam name="TGame">
    ///     The type of game to manage.
    /// </typeparam>
    /// <typeparam name="TPlayer">
    ///     The type of the <see cref="Player"/> object.
    /// </typeparam>
    public abstract class ShardedMpGameService<TServer, TGame, TPlayer> : MpGameService<TGame, TPlayer>
        where TServer : RemoteMpGameServer<TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        //private readonly RemoteMpGameServer<TGame, TPlayer> _server;
        private readonly ConcurrentDictionary<IMessageChannel, TServer> _servers
            = new(MessageChannelComparer);

        /// <summary>
        ///     Instantiates the MpGameService for the specified Game and Player type.
        /// </summary>
        /// <param name="client">
        ///     The Discord client.
        /// </param>
        /// <param name="mpconfig">
        ///     An optional config type.
        /// </param>
        /// <param name="logger">
        ///     An optional logging method.
        /// </param>
        public ShardedMpGameService(
            DiscordSocketClient client,
            IMpGameServiceConfig? mpconfig = null,
            Func<LogMessage, Task>? logger = null)
            : base(client, mpconfig, logger)
        {
        }

        /// <summary>
        ///     Gets the gRPC channel that will be used to communicate to the specified shard.
        /// </summary>
        /// <param name="shardId">
        ///     The shard that is to be connected to.
        /// </param>
        protected abstract ChannelBase GetChannelForShard(int shardId);

        internal bool TryGetServer(IMessageChannel channel, [NotNullWhen(true)] out TServer? server)
        {
            var chan = (channel is IDMChannel dm && TryGetGameChannel(dm, out var pubc))
                ? pubc : channel;

            return _servers.TryGetValue(chan, out server);
        }
    }

    /// <summary>
    ///     Service managing games for <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/>
    ///     in a multi-process sharded way with the default <see cref="Player"/> type.
    /// </summary>
    /// <typeparam name="TGame">
    ///     The type of game to manage.
    /// </typeparam>
    public abstract class ShardedMpGameService<TServer, TGame> : ShardedMpGameService<TServer, TGame, Player>
        where TServer : RemoteMpGameServer<TGame, Player>
        where TGame : GameBase<Player>
    {
        /// <summary>
        ///     Instantiates the MpGameService for the specified Game type.
        /// </summary>
        /// <param name="client">
        ///     The Discord client.
        /// </param>
        /// <param name="mpconfig">
        ///     An optional config type.
        /// </param>
        /// <param name="logger">
        ///     An optional logging method.
        /// </param>
        public ShardedMpGameService(
            DiscordSocketClient client,
            IMpGameServiceConfig? mpconfig = null,
            Func<LogMessage, Task>? logger = null)
            : base(client, mpconfig, logger)
        {
        }
    }
}
