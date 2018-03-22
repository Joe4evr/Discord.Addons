using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Addons.MpGame
{
    /// <summary> Base class to represent a game between Discord users. </summary>
    /// <typeparam name="TPlayer">The type of this game's kind of <see cref="Player"/> object.</typeparam>
    public abstract class GameBase<TPlayer>
        where TPlayer : Player
    {
        /// <summary> Sets up the common logic for a multiplayer game. </summary>
        /// <param name="channel">The channel where the public-facing side of the game is played.</param>
        /// <param name="players">The players for this game instance.</param>
        /// <param name="setFirstPlayerImmediately">When set to <see langword="true"/>, will set the TurnPlayer
        /// to the first player before the game begins, otherwise it will be set to an empty Node and you will
        /// have to set it to Turnplayer.Next when starting the first turn.</param>
        protected GameBase(IMessageChannel channel, IEnumerable<TPlayer> players, bool setFirstPlayerImmediately = false)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));

            Players = new CircularLinkedList<TPlayer>(players, MpGameComparers.PlayerComparer);
            TurnPlayer = setFirstPlayerImmediately ? Players.Head : Node<TPlayer>.CreateNextOnlyNode(Players.Head);
        }

        /// <summary> The channel where the public-facing side of the game is played. </summary>
        protected IMessageChannel Channel { get; }

        /// <summary> Represents all the players in this game. </summary>
        protected internal CircularLinkedList<TPlayer> Players { get; }

        /// <summary> The current turn's player. </summary>
        public Node<TPlayer> TurnPlayer { get; protected set; }

        ///// <summary> Indicates whether or not the current TurnPlayer is the first player in the list. </summary>
        //protected bool IsTurnPlayerFirstPlayer() => Players.Comparer.Equals(TurnPlayer.Value, Players.Head.Value);

        /// <summary> Indicates whether or not the current TurnPlayer is the last player in the list. </summary>
        protected bool IsTurnPlayerLastPlayer() => Players.Comparer.Equals(TurnPlayer.Value, Players.Tail.Value);

        ///// <summary> Selects the DM Channels of all the players. </summary>
        //public async Task<IEnumerable<IDMChannel>> PlayerChannels()
        //    => await Task.WhenAll(Players.Select(async p => await p.User.GetOrCreateDMChannelAsync().ConfigureAwait(false))).ConfigureAwait(false);

        //protected async Task SendMessageAllPlayers(Func<TPlayer, string> messageFunc)
        //{
        //    foreach (var player in Players)
        //    {
        //        await player.SendMessageAsync(messageFunc(player)).ConfigureAwait(false);
        //        await Task.Delay(1000).ConfigureAwait(false);
        //    }
        //}

        /// <summary> Perform the actions that are part of the initial setup. </summary>
        public abstract Task SetupGame();

        /// <summary> Perform the one-time actions that happen at the start of the game. </summary>
        public abstract Task StartGame();

        /// <summary> Perform all actions that are part of starting a new turn. </summary>
        public abstract Task NextTurn();

        /// <summary> Perform all actions that happen when the game ends
        /// (e.g.: a win condition is met, or the game is stopped early). </summary>
        /// <param name="endmsg">The message that should be displayed announcing
        /// the win condition or forced end of the game.</param>
        public virtual async Task EndGame(string endmsg)
        {
            await Channel.SendMessageAsync(endmsg).ConfigureAwait(false);
            await _gameEnd(Channel).ConfigureAwait(false);
        }

        /// <summary> Get a string that represent the state of the game. </summary>
        public abstract string GetGameState();
        //public abstract Embed GetGameStateEmbed();

        private Func<IMessageChannel, Task> _gameEnd;
        internal Func<IMessageChannel, Task> GameEnd { set => _gameEnd = value; }
    }
}
