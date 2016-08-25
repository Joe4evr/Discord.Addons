using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

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
        /// A cached instance of <see cref="IEqualityComparer{IGuildUser}"/> to use when
        /// instantiating the <see cref="PlayerList"/>'s <see cref="HashSet{IGuildUser}"/>.
        /// </summary>
        protected static readonly IEqualityComparer<IGuildUser> UserComparer = new EntityEqualityComparer<ulong>();

        /// <summary>
        /// The instance of a game being played, keyed by channel ID.
        /// </summary>
        protected readonly ConcurrentDictionary<ulong, TGame> GameList = new ConcurrentDictionary<ulong, TGame>();

        /// <summary>
        /// The list of users scheduled to join game, keyed by channel ID.
        /// </summary>
        /// <remarks>
        /// When instantiating the <see cref="HashSet{IGuildUser}"/>,
        /// pass in <see cref="UserComparer"/> for the <see cref="IEqualityComparer{IGuildUser}"/>.
        /// </remarks>
        protected readonly ConcurrentDictionary<ulong, HashSet<IGuildUser>> PlayerList
            = new ConcurrentDictionary<ulong, HashSet<IGuildUser>>();

        /// <summary>
        /// Indicates whether the users can join a game about to start, keyed by channel ID.
        /// </summary>
        protected readonly ConcurrentDictionary<ulong, bool> OpenToJoin = new ConcurrentDictionary<ulong, bool>();

        /// <summary>
        /// Indicates whether a game is currently going on, keyed by channel ID.
        /// </summary>
        protected readonly ConcurrentDictionary<ulong, bool> GameInProgress = new ConcurrentDictionary<ulong, bool>();

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

        /// <summary>
        /// Command to resend a message to someone who had their DMs disabled.
        /// </summary>
        [Command("resend")]
        public async Task ResendCmd(IMessage msg)
        {
            bool gip;
            TGame game;
            if (GameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip
                && GameList.TryGetValue(msg.Channel.Id, out game))
            {
                var player = game.Players.SingleOrDefault(p => p.User.Id == msg.Author.Id);
                if (player != null)
                {
                    await game.TryResendDMAsync(player);
                }
            }
        }
    }
}
