using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord.Net;

namespace Discord.Addons.MpGame
{
    /// <summary>
    /// Base class to represent a game between Discord users.
    /// </summary>
    /// <typeparam name="TPlayer">The type of this game's kind of <see cref="Player"/> object.</typeparam>
    public abstract class GameBase<TPlayer> where TPlayer : Player
    {
        /// <summary>
        /// The channel where the public-facing side of the game is played.
        /// </summary>
        protected internal IMessageChannel Channel { get; }

        /// <summary>
        /// Represents all the players in this game.
        /// </summary>
        protected internal CircularLinkedList<TPlayer> Players { get; }

        /// <summary>
        /// Selects the DM Channels of all the players.
        /// </summary>
        public IEnumerable<IDMChannel> PlayerChannels => Players.Select(p => p.DmChannel);

        /// <summary>
        /// The current turn's player.
        /// </summary>
        protected Node<TPlayer> TurnPlayer { get; set; }

        /// <summary>
        /// Sets up the common logic for a multiplayer game.
        /// </summary>
        protected GameBase(IMessageChannel channel, IEnumerable<TPlayer> players)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (players == null) throw new ArgumentNullException(nameof(players));

            Channel = channel;
            Players = new CircularLinkedList<TPlayer>(players);
        }

        /// <summary>
        /// Perform the actions that are part of the initial setup.
        /// </summary>
        public abstract Task SetupGame();

        /// <summary>
        /// Perform the one-time actions that happen at the start of the game.
        /// </summary>
        public abstract Task StartGame();

        /// <summary>
        /// Perform all actions that are part of starting a new turn.
        /// </summary>
        public abstract Task NextTurn();

        /// <summary>
        /// Perform all actions that happen when the game ends
        /// (e.g.: a win condition is met, or the game is stopped early).
        /// </summary>
        /// <param name="endmsg">The message that should be displayed announcing
        /// the win condition or forced end of the game.</param>
        public virtual async Task EndGame(string endmsg)
        {
            await Channel.SendMessageAsync(endmsg);
            await GameEnd(Channel.Id);
        }

        /// <summary>
        /// Get a string that represent the state of the game.
        /// </summary>
        public abstract string GetGameState();

        internal event Func<ulong, Task> GameEnd;
    }
}
