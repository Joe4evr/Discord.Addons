using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Discord.Addons.MpGame
{
    /// <summary>
    /// Base class to manage a game between Discord users.
    /// </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    /// <remarks>Inheriting classes should be marked with
    /// <see cref="Commands.ModuleAttribute"/>.</remarks>
    public abstract class MpGameModuleBase<TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        /// <summary>
        /// The instance of a game being played, keyed by channel ID.
        /// </summary>
        protected readonly ConcurrentDictionary<ulong, TGame> gameList = new ConcurrentDictionary<ulong, TGame>();

        /// <summary>
        /// The list of users scheduled to join game, keyed by channel ID.
        /// </summary>
        protected readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IGuildUser>> playerList
            = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IGuildUser>>();

        /// <summary>
        /// Indicates whether the users can join a game about to start, keyed by channel ID.
        /// </summary>
        protected readonly ConcurrentDictionary<ulong, bool> openToJoin = new ConcurrentDictionary<ulong, bool>();

        /// <summary>
        /// Indicates whether a game is currently going on, keyed by channel ID.
        /// </summary>
        protected readonly ConcurrentDictionary<ulong, bool> gameInProgress = new ConcurrentDictionary<ulong, bool>();

        /// <summary>
        /// Sets up the common logic for a Module that manages a game between Discord users.
        /// </summary>
        /// <remarks>Automatically subscribes a handler to
        /// <see cref="DiscordSocketClient.MessageReceived"/>.</remarks>
        protected MpGameModuleBase(DiscordSocketClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            client.MessageReceived += clientMessageReceived;
        }

        /// <summary>
        /// The handler that responds to messages during a game.
        /// </summary>
        private async Task clientMessageReceived(IMessage msg)
        {
            bool gip;
            TGame game;
            if (gameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip &&
                gameList.TryGetValue(msg.Channel.Id, out game))
            {
                var dmch = msg.Channel as IDMChannel;
                if (dmch != null && game.Players.Any(p => p.DmChannel.Id == dmch.Id))
                {
                    //message was a player's PM
                    await game.OnDmMessage(msg);
                }
                else if (msg.Channel.Id == game.Channel.Id)
                {
                    //message was in the public channel
                    await game.OnPublicMessage(msg);
                }
            }
        }

        /// <summary>
        /// Command to open a game for others to join.
        /// </summary>
        public abstract Task OpenGameCmd(IMessage msg);

        /// <summary>
        /// Command to cancel a game before it started.
        /// </summary>
        public abstract Task CancelGameCmd(IMessage msg);

        /// <summary>
        /// Command to join a game that is open.
        /// </summary>
        public abstract Task JoinGameCmd(IMessage msg);

        /// <summary>
        /// Command to leave a game that is not yet started.
        /// </summary>
        public abstract Task LeaveGameCmd(IMessage msg);

        /// <summary>
        /// Command to start a game with the players who joined.
        /// </summary>
        public abstract Task StartGameCmd(IMessage msg);

        /// <summary>
        /// Command to advance to the next turn (if applicable).
        /// </summary>
        public abstract Task NextTurnCmd(IMessage msg);

        /// <summary>
        /// Command to display the current state of the game.
        /// </summary>
        public abstract Task GameStateCmd(IMessage msg);

        /// <summary>
        /// Command to end a game in progress early.
        /// </summary>
        public abstract Task EndGameCmd(IMessage msg);
    }
}
