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
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer> : ModuleBase<SocketCommandContext>
        where TService : MpGameService<TGame, TPlayer>
        where TGame    : GameBase<TPlayer>
        where TPlayer  : Player
    {
        /// <summary>
        ///     The GameService instance.
        /// </summary>
        protected TService GameService { get; }

        /// <summary>
        ///     Initializes the <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/> base class.
        /// </summary>
        /// <param name="gameService">
        ///     The <typeparamref name="TService"/> instance.
        /// </param>
        protected MpGameModuleBase(TService gameService)
        {
            GameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        }

        // TODO: C# "who-knows-when" feature, nullability annotation
        /// <summary>
        ///     The instance of the game being played (if active).
        /// </summary>
        protected TGame Game { get; private set; }

        /// <summary>
        ///     The player object that wraps the user executing this command
        ///     (if a game is active AND the user is a player in that game).
        /// </summary>
        protected TPlayer Player { get; private set; }

        /// <summary>
        ///     Determines if a game in the current channel is in progress or not.
        /// </summary>
        protected internal CurrentlyPlaying GameInProgress { get; private set; } = CurrentlyPlaying.None;

        /// <summary>
        ///     Determines if a game in the current channel is open to join or not.
        /// </summary>
        protected bool OpenToJoin { get; private set; } = false;

        /// <summary>
        ///     The list of users ready to play.
        /// </summary>
        /// <remarks>
        ///     <note type="note">
        ///         This is an immutable snapshot, it is not updated until the *next* command invocation.
        ///     </note>
        /// </remarks>
        protected IReadOnlyCollection<IUser> JoinedUsers { get; private set; } = ImmutableHashSet<IUser>.Empty;

        /// <summary>
        ///     Initialize fields whose values come from the <typeparamref name="TService"/>'s Dictionaries.
        /// </summary>
        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);

            var data = GameService.GetGameData(Context);
            OpenToJoin     = data.OpenToJoin;
            JoinedUsers    = data.JoinedUsers;
            Game           = data.Game;
            Player         = data.Player;
            GameInProgress = data.GameInProgress;
        }

        /// <summary>
        ///     Override this to return <see langword="false"/> if you don't want to register a type reader for the <typeparamref name="TPlayer"/> type.
        /// </summary>
        protected virtual bool RegisterPlayerTypeReader => true;

        /// <inheritdoc/>
        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            base.OnModuleBuilding(commandService, builder);

            if (RegisterPlayerTypeReader)
            {
                GameService.LogRegisteringPlayerTypeReader();
                commandService.AddTypeReader<TPlayer>(new PlayerTypeReader());
            }
        }

        /// <summary>
        ///     Command to open a game for others to join.
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         [Command("opengame")]
        ///         public override async Task OpenGameCmd()
        ///         {
        ///             if (OpenToJoin)
        ///             {
        ///                 await ReplyAsync("There is already a game open to join.").ConfigureAwait(false);
        ///             }
        ///             else if (GameInProgress != CurrentlyPlaying.None)
        ///             {
        ///                 await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
        ///             }
        ///             else
        ///             {
        ///                 if (await GameService.OpenNewGame(Context).ConfigureAwait(false))
        ///                 {
        ///                     await ReplyAsync("Opening for a game.").ConfigureAwait(false);
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        public abstract Task OpenGameCmd();

        /// <summary>
        ///     Command to join a game that is open.
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         [Command("join")]
        ///         public override async Task JoinGameCmd()
        ///         {
        ///             if (Game != null)
        ///             {
        ///                 await ReplyAsync("Cannot join a game already in progress.").ConfigureAwait(false);
        ///             }
        ///             else if (!OpenToJoin)
        ///             {
        ///                 await ReplyAsync("No game open to join.").ConfigureAwait(false);
        ///             }
        ///             else
        ///             {
        ///                 if (await GameService.AddUser(Context.Channel, Context.User).ConfigureAwait(false))
        ///                 {
        ///                     await ReplyAsync($"**{Context.User.Username}** has joined.").ConfigureAwait(false);
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        public abstract Task JoinGameCmd();

        /// <summary>
        ///     Command to leave a game that is not yet started.
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         [Command("leave")]
        ///         public override async Task LeaveGameCmd()
        ///         {
        ///             if (Game != null)
        ///             {
        ///                 await ReplyAsync("Cannot leave a game already in progress.").ConfigureAwait(false);
        ///             }
        ///             else if (!OpenToJoin)
        ///             {
        ///                 await ReplyAsync("No game open to leave.").ConfigureAwait(false);
        ///             }
        ///             else
        ///             {
        ///                 if (await GameService.RemoveUser(Context.Channel, Context.User))
        ///                 {
        ///                     await ReplyAsync($"**{Context.User.Username}** has left.").ConfigureAwait(false);
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        public abstract Task LeaveGameCmd();

        /// <summary>
        ///     Command to cancel a game before it started.
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         [Command("cancel")]
        ///         public override async Task CancelGameCmd()
        ///         {
        ///             if (Game != null)
        ///             {
        ///                 await ReplyAsync("Cannot cancel a game already in progress.").ConfigureAwait(false);
        ///             }
        ///             else if (!OpenToJoin)
        ///             {
        ///                 await ReplyAsync("No game open to cancel.").ConfigureAwait(false);
        ///             }
        ///             else
        ///             {
        ///                 if (await GameService.CancelGame(Context.Channel))
        ///                 {
        ///                     await ReplyAsync("Game was canceled.").ConfigureAwait(false);
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        public abstract Task CancelGameCmd();

        /// <summary>
        ///     Command to start a game with the players who joined.
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         [Command("start")]
        ///         public override async Task StartGameCmd()
        ///         {
        ///             if (Game != null)
        ///             {
        ///                 await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
        ///             }
        ///             else if (!OpenToJoin)
        ///             {
        ///                 await ReplyAsync("No game has been opened at this time.").ConfigureAwait(false);
        ///             }
        ///             else if (JoinedUsers.Count &lt; 2) // Example value if a game has a minimum player requirement
        ///             {
        ///                 await ReplyAsync("Not enough players have joined.").ConfigureAwait(false);
        ///             }
        ///             else
        ///             {
        ///                 if (GameService.TryUpdateOpenToJoin(Context.Channel, newValue: false, comparisonValue: true))
        ///                 {
        ///                     // Tip: Shuffle the players before projecting them
        ///                     var players = JoinedUsers.Select(u =&gt; new ExamplePlayer(u, Context.Channel));
        ///                     // The Player class can also be extended for additional properties
        ///                     var game = new ExampleGame(Context.Channel, players);
        ///                     if (await GameService.TryAddNewGame(Context.Channel, game).ConfigureAwait(false))
        ///                     {
        ///                         await game.SetupGame().ConfigureAwait(false);
        ///                         await game.StartGame().ConfigureAwait(false);
        ///                     }
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        public abstract Task StartGameCmd();

        /// <summary>
        ///     Command to advance to the next turn (if applicable).
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         [Command("turn")]
        ///         public override Task NextTurnCmd()
        ///             => (Game != null)
        ///                 ? Game.NextTurn()
        ///                 : (GameInProgress == CurrentlyPlaying.DifferentGame)
        ///                     ? ReplyAsync("Different game in progress.")
        ///                     : ReplyAsync("No game in progress.");
        ///     </code>
        /// </example>
        public abstract Task NextTurnCmd();

        /// <summary>
        ///     Command to display the current state of the game.
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         [Command("state")]
        ///         public override Task GameStateCmd()
        ///             => (Game != null)
        ///                 ? ReplyAsync(Game.GetGameState())
        ///                 //Alternatively: ReplyAsync("", embed: Game.GetGameStateEmbed())
        ///                 : (GameInProgress == CurrentlyPlaying.DifferentGame)
        ///                     ? ReplyAsync("Different game in progress.")
        ///                     : ReplyAsync("No game in progress.");
        ///     </code>
        /// </example>
        public abstract Task GameStateCmd();

        /// <summary>
        ///     Command to end a game in progress early.
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         [Command("end")] //Should be restricted to mods/admins to prevent abuse
        ///         public override Task EndGameCmd()
        ///             => (Game != null)
        ///                 ? Game.EndGame("Game ended early by moderator.")
        ///                 : GameInProgress == CurrentlyPlaying.DifferentGame
        ///                     ? ReplyAsync("Different game in progress.")
        ///                     : ReplyAsync("No game in progress.");
        ///     </code>
        /// </example>
        public abstract Task EndGameCmd();

        /// <summary>
        ///     Command to resend a message to someone who had their DMs disabled.
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         [Command("resend")]
        ///         public override Task ResendCmd() => base.ResendCmd();
        ///     </code>
        /// </example>
        public virtual async Task ResendCmd()
        {
            if (Player != null)
                await Player.RetrySendMessagesAsync();
        }
    }

    /// <summary>
    ///     Base class to manage a game between Discord users, using the default <see cref="MpGameService{TGame, TPlayer}"/> type.
    /// </summary>
    /// <typeparam name="TGame">
    ///     The type of game to manage.
    /// </typeparam>
    /// <typeparam name="TPlayer">
    ///     The type of the <see cref="Player"/> object.
    /// </typeparam>
    public abstract class MpGameModuleBase<TGame, TPlayer> : MpGameModuleBase<MpGameService<TGame, TPlayer>, TGame, TPlayer>
        where TGame   : GameBase<TPlayer>
        where TPlayer : Player
    {
        /// <inheritdoc/>
        protected MpGameModuleBase(MpGameService<TGame, TPlayer> service)
            : base(service)
        {
        }
    }

    /// <summary>
    ///     Base class to manage a game between Discord users, using the default <see cref="MpGameService{TGame, Player}"/> and <see cref="Player"/> types.
    /// </summary>
    /// <typeparam name="TGame">
    ///     The type of game to manage.
    /// </typeparam>
    public abstract class MpGameModuleBase<TGame> : MpGameModuleBase<MpGameService<TGame, Player>, TGame, Player>
        where TGame : GameBase<Player>
    {
        /// <inheritdoc/>
        protected MpGameModuleBase(MpGameService<TGame> service)
            : base(service)
        {
        }
    }
}
