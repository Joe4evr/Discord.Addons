using System;
using System.Collections.Generic;
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
    public abstract class MpGameModuleBase<TService, TGame, TPlayer> : ModuleBase<SocketCommandContext>
        where TService : MpGameService<TGame, TPlayer>
        where TGame    : GameBase<TPlayer>
        where TPlayer  : Player
    {
        /// <summary> Initializes the <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/> base class. </summary>
        /// <param name="gameService"></param>
        protected MpGameModuleBase(TService gameService)
        {
            GameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        }

        /// <summary> Initialize fields whose values come from the <see cref="TService"/>'s Dictionaries. </summary>
        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
            var data = GameService.GetData(Context.Channel);
            OpenToJoin = data?.OpenToJoin ?? false;
            PlayerList = data?.JoinedUsers ?? ImmutableHashSet<IUser>.Empty;
            Game = data?.Game;
            GameInProgress = GameTracker.Instance.TryGet(Context.Channel, out var name)
                ? (name == GameService.GameName ? CurrentlyPlaying.ThisGame : CurrentlyPlaying.DifferentGame)
                : CurrentlyPlaying.None;
        }

        /// <summary> The <see cref="TService"/> instance. </summary>
        protected TService GameService { get; }

        //TODO: C# "who-knows-when" feature, nullability annotation
        /// <summary> The instance of the game being played (if active). </summary>
        protected TGame Game { get; private set; }

        /// <summary> Determines if a game in the current channel is in progress or not. </summary>
        protected CurrentlyPlaying GameInProgress { get; private set; }

        /// <summary> Determines if a game in the current channel is open to join or not. </summary>
        protected bool OpenToJoin { get; private set; }

        /// <summary> The list of users ready to play. </summary>
        protected IReadOnlyCollection<IUser> PlayerList { get; private set; }

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
        //[Command("resend")]
        public virtual async Task ResendCmd()
        {
            if (GameInProgress == CurrentlyPlaying.ThisGame)
            {
                var player = Game.Players.SingleOrDefault(p => p.User.Id == Context.User.Id);
                if (player != null)
                {
                    await player.RetrySendMessageAsync();
                }
            }
        }

        /// <summary> Specifies if a game is being played when the command is invoked. </summary>
        protected enum CurrentlyPlaying
        {
            /// <summary> No game is being played in this channel. </summary>
            None = 0,

            /// <summary> This game is being played in this channel. </summary>
            ThisGame = 1,

            /// <summary> A different game is being played in this channel. </summary>
            DifferentGame = 2
        }
    }

    /// <summary> Base class to manage a game between Discord users,
    /// using the default <see cref="MpGameService{TGame, TPlayer}"/> type. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    public abstract class MpGameModuleBase<TGame, TPlayer> : MpGameModuleBase<MpGameService<TGame, TPlayer>, TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        protected MpGameModuleBase(MpGameService<TGame, TPlayer> service) : base(service)
        {
        }
    }

    /// <summary> Base class to manage a game between Discord users,
    /// using the default <see cref="MpGameService{TGame, Player}"/>
    /// and <see cref="Player"/> types. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    public abstract class MpGameModuleBase<TGame> : MpGameModuleBase<MpGameService<TGame, Player>, TGame, Player>
        where TGame : GameBase<Player>
    {
        protected MpGameModuleBase(MpGameService<TGame> service) : base(service)
        {
        }
    }
}
