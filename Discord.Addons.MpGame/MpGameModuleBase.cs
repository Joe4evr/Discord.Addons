using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    /// <summary> Base class to manage a game between Discord users. </summary>
    /// <typeparam name="TService">The type of the service managing longer lived objects.</typeparam>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    public abstract class MpGameModuleBase<TService, TGame, TPlayer> : ModuleBase
        where TService : MpGameService<TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        /// <summary> The instance of the game being played (if active). </summary>
        protected TGame Game => _game;

        /// <summary> Determines if a game in the current channel is in progress or not. </summary>
        protected bool GameInProgress { get; }

        /// <summary> The <see cref="MpGameService{TGame, TPlayer}"/> instance. </summary>
        protected TService GameService { get; }

        /// <summary> Determines if a game in the current channel is open to join or not. </summary>
        protected bool OpenToJoin => _open;

        /// <summary> The list of users ready to play. </summary>
        protected ImmutableHashSet<IUser> PlayerList => _list;

        private readonly bool _open;
        private readonly TGame _game;
        private readonly ImmutableHashSet<IUser> _list;

        /// <summary> Initializes the <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/> base class. </summary>
        /// <param name="gameService"></param>
        protected MpGameModuleBase(TService gameService)
        {
            if (gameService == null) throw new ArgumentNullException(nameof(gameService));

            gameService.OpenToJoin.TryGetValue(Context.Channel.Id, out _open);
            gameService.PlayerList.TryGetValue(Context.Channel.Id, out _list);

            GameInProgress = gameService.GameList.TryGetValue(Context.Channel.Id, out _game);
            
            GameService = gameService;
        }

        /// <summary> Command to open a game for others to join. </summary>
        public abstract Task OpenGameCmd();

        /// <summary> Command to cancel a game before it started. </summary>
        public abstract Task CancelGameCmd();

        /// <summary> Command to join a game that is open. </summary>
        public abstract Task JoinGameCmd();

        /// <summary> Command to leave a game that is not yet started. </summary>
        public abstract Task LeaveGameCmd();

        /// <summary> Command to start a game with the players who joined. </summary>
        public abstract Task StartGameCmd();

        /// <summary> Command to advance to the next turn (if applicable). </summary>
        public abstract Task NextTurnCmd();

        /// <summary> Command to display the current state of the game. </summary>
        public abstract Task GameStateCmd();

        /// <summary> Command to end a game in progress early. </summary>
        public abstract Task EndGameCmd();

        /// <summary> Command to resend a message to someone who had their DMs disabled. </summary>
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
