using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Commands.Builders;

namespace Discord.Addons.MpGame
{
    /// <summary> Base class to manage a game between Discord users. </summary>
    /// <typeparam name="TService">The type of the service managing longer lived objects.</typeparam>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer> : ModuleBase<ICommandContext>
        where TService : MpGameService<TGame, TPlayer>
        where TGame    : GameBase<TPlayer>
        where TPlayer  : Player
    {
        /// <summary> The GameService instance. </summary>
        protected TService GameService { get; }

        /// <summary> Initializes the <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/> base class. </summary>
        /// <param name="gameService"></param>
        protected MpGameModuleBase(TService gameService)
        {
            GameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        }

        // TODO: C# "who-knows-when" feature, nullability annotation
        /// <summary> The instance of the game being played (if active). </summary>
        protected TGame Game { get; private set; }

        /// <summary> The player object that wraps the user executing this command
        /// (if a game is active AND the user is a player in that game). </summary>
        protected TPlayer Player { get; private set; }

        /// <summary> Determines if a game in the current channel is in progress or not. </summary>
        protected internal CurrentlyPlaying GameInProgress { get; private set; } = CurrentlyPlaying.None;

        /// <summary> Determines if a game in the current channel is open to join or not. </summary>
        protected bool OpenToJoin { get; private set; } = false;

        /// <summary> The list of users ready to play. </summary>
        protected IReadOnlyCollection<IUser> JoinedUsers { get; private set; } = ImmutableHashSet<IUser>.Empty;

        /// <summary> Initialize fields whose values come from the <see cref="TService"/>'s Dictionaries. </summary>
        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);

            if (GameService.TryGetPersistentData(Context.Channel, out var data))
            {
                OpenToJoin  = data.OpenToJoin;
                JoinedUsers = data.JoinedUsers;
                Game        = data.Game;
                Player      = Game?.Players.SingleOrDefault(p => p.User.Id == Context.User.Id);
            }

            GameInProgress = GameTracker.Instance.TryGetGameString(Context.Channel, out var name)
                ? (name == GameService.GameName ? CurrentlyPlaying.ThisGame : CurrentlyPlaying.DifferentGame)
                : CurrentlyPlaying.None;

            // Prep C# 8.0 pattern matching feature: switch expression
            // Sure, it'll compile into the exact code I already have,
            // BUT LOOKIT HOW CLEAN IT IS!
            //GameInProgress = GameTracker.Instance.TryGetGameString(Context.Channel, out var name) switch
            //{
            //    true when name == GameService.GameName => CurrentlyPlaying.ThisGame,
            //    true  => CurrentlyPlaying.DifferentGame,
            //    false => CurrentlyPlaying.None
            //};
        }

        /// <summary> Override this to return <see langword="false"/> if you don't
        /// want to register a type reader for the <see cref="TPlayer"/> type.</summary>
        protected virtual bool RegisterPlayerTypeReader => true;

        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            base.OnModuleBuilding(commandService, builder);

            if (RegisterPlayerTypeReader)
            {
                GameService.Logger(new LogMessage(LogSeverity.Info, MpGameService<TGame, TPlayer>.LogSource, $"Registering type reader for {typeof(TPlayer).Name}"));
                commandService.AddTypeReader<TPlayer>(new PlayerTypeReader());
            }
        }

        /// <summary> Command to open a game for others to join. </summary>
        /// <example> public override async Task OpenGameCmd()
        /// {
        ///     if (OpenToJoin)
        ///     {
        ///         await ReplyAsync("There is already a game open to join.").ConfigureAwait(false);
        ///     }
        ///     else if (GameInProgress != CurrentlyPlaying.None)
        ///     {
        ///         await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
        ///     }
        ///     else
        ///     {
        ///         if (await GameService.OpenNewGame(Context).ConfigureAwait(false))
        ///         {
        ///             await ReplyAsync("Opening for a game.").ConfigureAwait(false);
        ///         }
        ///     }
        /// }
        /// </example>
        public abstract Task OpenGameCmd();

        /// <summary> Command to join a game that is open. </summary>
        public abstract Task JoinGameCmd();

        /// <summary> Command to leave a game that is not yet started. </summary>
        public abstract Task LeaveGameCmd();

        /// <summary> Command to cancel a game before it started. </summary>
        public abstract Task CancelGameCmd();

        /// <summary> Command to start a game with the players who joined. </summary>
        public abstract Task StartGameCmd();

        /// <summary> Command to advance to the next turn (if applicable). </summary>
        public abstract Task NextTurnCmd();

        /// <summary> Command to display the current state of the game. </summary>
        public abstract Task GameStateCmd();

        /// <summary> Command to end a game in progress early. </summary>
        public abstract Task EndGameCmd();

        /// <summary> Command to resend a message to someone who had their DMs disabled. </summary>
        public virtual Task ResendCmd()
        {
            return (GameInProgress == CurrentlyPlaying.ThisGame && Player != null)
                ? Player.RetrySendMessagesAsync()
                : Task.CompletedTask;
        }
    }

    /// <summary> Base class to manage a game between Discord users,
    /// using the default <see cref="MpGameService{TGame, TPlayer}"/> type. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    public abstract class MpGameModuleBase<TGame, TPlayer> : MpGameModuleBase<MpGameService<TGame, TPlayer>, TGame, TPlayer>
        where TGame   : GameBase<TPlayer>
        where TPlayer : Player
    {
        protected MpGameModuleBase(MpGameService<TGame, TPlayer> service)
            : base(service)
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
        protected MpGameModuleBase(MpGameService<TGame> service)
            : base(service)
        {
        }
    }
}
