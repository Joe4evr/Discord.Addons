using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Addons.MpGame.Collections;

namespace Discord.Addons.MpGame
{
    /// <summary>
    ///     Base class to represent a game between Discord users.
    /// </summary>
    /// <typeparam name="TPlayer">
    ///     The type of this game's kind of <see cref="Player"/> object.
    /// </typeparam>
    public abstract class GameBase<TPlayer>
        where TPlayer : Player
    {
        /// <summary>
        ///     Sets up the common logic for a multiplayer game.
        /// </summary>
        /// <param name="channel">
        ///     The channel where the public-facing side of the game is played.
        /// </param>
        /// <param name="players">
        ///     The players for this game instance.
        /// </param>
        /// <param name="setFirstPlayerImmediately">
        ///     When set to <see langword="true"/>, will set the TurnPlayer to the first player before the game begins,
        ///     otherwise it will be set to an empty Node and you will have to set it to Turnplayer.Next when starting the first turn.
        /// </param>
        protected GameBase(
            IMessageChannel channel,
            IEnumerable<TPlayer> players,
            bool setFirstPlayerImmediately = false)
        {
            if (players is null) throw new ArgumentNullException(nameof(players));
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));

            Players = new CircularLinkedList<TPlayer>(players, MpGameComparers.PlayerComparer);
            TurnPlayer = setFirstPlayerImmediately ? Players.Head : Node<TPlayer>.CreateNextOnlyNode(Players.Head);
        }

        /// <summary>
        ///     The channel where the public-facing side of the game is played.
        /// </summary>
        protected internal IMessageChannel Channel { get; }

        /// <summary>
        ///     Represents all the players in this game.
        /// </summary>
        protected internal CircularLinkedList<TPlayer> Players { get; }

        /// <summary>
        ///     The current turn's player.
        /// </summary>
        public Node<TPlayer> TurnPlayer { get; protected set; }

        /// <summary>
        ///     Indicates whether or not the current TurnPlayer is the first player in the list.
        /// </summary>
        protected bool IsTurnPlayerFirstPlayer()
            => Players.Comparer.Equals(TurnPlayer.Value, Players.Head.Value);

        /// <summary>
        ///     Indicates whether or not the current TurnPlayer is the last player in the list.
        /// </summary>
        protected bool IsTurnPlayerLastPlayer()
            => Players.Comparer.Equals(TurnPlayer.Value, Players.Tail.Value);

        /// <summary>
        ///     Perform the actions that are part of the initial setup.
        /// </summary>
        public abstract Task SetupGame();

        /// <summary>
        ///     Perform the one-time actions that happen at the start of the game.
        /// </summary>
        public abstract Task StartGame();

        /// <summary>
        ///     Perform all actions that are part of starting a new turn.
        /// </summary>
        public abstract Task NextTurn();

        /// <summary>
        ///     Perform all actions that happen when the game ends (e.g.: a win condition is met, or the game is stopped early).
        /// </summary>
        /// <param name="endmsg">
        ///     The message that should be displayed announcing the win condition or forced end of the game.
        /// </param>
        public async Task EndGame(string endmsg)
        {
            await OnGameEnd();

            await Channel.SendMessageAsync(endmsg).ConfigureAwait(false);
            await GameEnd(Channel).ConfigureAwait(false);
        }

        protected virtual Task OnGameEnd() => Task.CompletedTask;

        /// <summary>
        ///     Get a string that represents the state of the game.
        /// </summary>
        /// <remarks>
        ///     <note type="implement">
        ///         Does not <i>need</i> to be implemented if only <see cref="GetGameStateEmbed"/> is used.
        ///     </note>
        /// </remarks>
        public abstract string GetGameState();

        /// <summary>
        ///     Get an embed that represents the state of the game.
        /// </summary>
        /// <remarks>
        ///     <note type="implement">
        ///         Does not <i>need</i> to be implemented if only <see cref="GetGameState"/> is used.
        ///     </note>
        /// </remarks>
        public abstract Embed GetGameStateEmbed();

        /// <summary>
        ///     Gets called when a player is added into an ongoing game, allowing an opportunity to add properties to the player.
        /// </summary>
        /// <param name="player">
        ///     The player that is added.
        /// </param>
        protected internal virtual void OnPlayerAdded(TPlayer player) { }

        /// <summary>
        ///     Gets called when a player is forcibly kicked, allowing an opportunity to access some of their properties to put back into the game.
        /// </summary>
        /// <param name="player">
        ///     The player that is removed.
        /// </param>
        protected internal virtual void OnPlayerKicked(TPlayer player) { }

        internal Func<IMessageChannel, Task> GameEnd { private get; set; } = _defaultend;
        private static readonly Func<IMessageChannel, Task> _defaultend = (_ => Task.CompletedTask);
    }
}
