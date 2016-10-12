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
    public abstract class MpGameModuleBase<TGame, TPlayer> : ModuleBase
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        /// <summary>
        /// A cached <see cref="IEqualityComparer{IGuildUser}"/> instance to use when
        /// instantiating the <see cref="PlayerList"/>'s <see cref="HashSet{IGuildUser}"/>.
        /// </summary>
        protected static readonly IEqualityComparer<IGuildUser> UserComparer = new EntityEqualityComparer<ulong>();

        /// <summary>
        /// Determines if a game in the current channel is open to join or not.
        /// </summary>
        protected readonly bool OpenToJoin;

        /// <summary>
        /// Determines if a game in the current channel is in progress or not.
        /// </summary>
        protected readonly bool GameInProgress;

        /// <summary>
        /// The instance of the game being played (if active).
        /// </summary>
        protected readonly TGame Game;

        /// <summary>
        /// The list of users ready to play.
        /// </summary>
        protected readonly HashSet<IGuildUser> PlayerList;

        /// <summary>
        /// The <see cref="MpGameService{TGame, TPlayer}"/> instance.
        /// </summary>
        protected readonly MpGameService<TGame, TPlayer> GameService;

        /// <summary>
        /// Initializes the <see cref="MpGameModuleBase{TGame, TPlayer}"/> base class.
        /// </summary>
        /// <param name="gameService"></param>
        protected MpGameModuleBase(MpGameService<TGame, TPlayer> gameService)
        {
            if (gameService == null) throw new ArgumentNullException(nameof(gameService));

            gameService.OpenToJoin.TryGetValue(Context.Channel.Id, out OpenToJoin);
            gameService.GameInProgress.TryGetValue(Context.Channel.Id, out GameInProgress);
            gameService.GameList.TryGetValue(Context.Channel.Id, out Game);
            gameService.PlayerList.TryGetValue(Context.Channel.Id, out PlayerList);
            GameService = gameService;
        }

        /// <summary>
        /// Command to open a game for others to join.
        /// </summary>
        public abstract Task OpenGameCmd();

        /// <summary>
        /// Command to cancel a game before it started.
        /// </summary>
        public abstract Task CancelGameCmd();

        /// <summary>
        /// Command to join a game that is open.
        /// </summary>
        public abstract Task JoinGameCmd();

        /// <summary>
        /// Command to leave a game that is not yet started.
        /// </summary>
        public abstract Task LeaveGameCmd();

        /// <summary>
        /// Command to start a game with the players who joined.
        /// </summary>
        public abstract Task StartGameCmd();

        /// <summary>
        /// Command to advance to the next turn (if applicable).
        /// </summary>
        public abstract Task NextTurnCmd();

        /// <summary>
        /// Command to display the current state of the game.
        /// </summary>
        public abstract Task GameStateCmd();

        /// <summary>
        /// Command to end a game in progress early.
        /// </summary>
        public abstract Task EndGameCmd();

        /// <summary>
        /// Command to resend a message to someone who had their DMs disabled.
        /// </summary>
        [Command("resend")]
        public async Task ResendCmd()
        {
            if (GameInProgress)
            {
                var player = Game.Players.SingleOrDefault(p => p.User.Id == Context.User.Id);
                if (player != null)
                {
                    await player.RetrySendMessageAsync();
                }
            }
        }
    }
}
