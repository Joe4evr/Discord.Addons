using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Discord.Addons.MpGame
{
    /// <summary>
    /// Base class to represent a game between Discord users.
    /// </summary>
    public abstract class GameBase
    {
        /// <summary>
        /// The channel where the public-facing side of the game is played.
        /// </summary>
        protected IMessageChannel Channel { get; }

        /// <summary>
        /// Represents all the players in this game,
        /// </summary.
        protected CircularLinkedList<Player> Players { get; }

        /// <summary>
        /// The current turn's player.
        /// </summary>
        public IGuildUser TurnPlayer { get; protected set; }

        /// <summary>
        /// The <see cref="DiscordSocketClient"/> instance.
        /// </summary>
        private readonly DiscordSocketClient _client;

        /// <summary>
        /// Sets up the common logic for a multiplayer game.
        /// </summary>
        /// <remarks>Automatically subscribes a handler to
        /// <see cref="DiscordSocketClient.MessageReceived"/>.</remarks>
        protected GameBase(IMessageChannel channel, IEnumerable<Player> players, DiscordSocketClient client)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (client == null) throw new ArgumentNullException(nameof(client));

            Channel = channel;
            Players = new CircularLinkedList<Player>(players);
            _client = client;
            client.MessageReceived += ProcessMessage;
        }

        /// <summary>
        /// The handler that responds to messages during a game.
        /// </summary>
        private async Task ProcessMessage(IMessage msg)
        {
            var dmch = msg.Channel as IDMChannel;
            if (dmch != null && Players.Select(p => p.DmChannel.Id).Any(id => id == dmch.Id))
            {
                //message was a player's PM
                await OnDmMessage(msg);
            }
            else if (msg.Channel.Id == Channel.Id)
            {
                //message was in the public channel
                await OnPublicMessage(msg);
            }
        }

        /// <summary>
        /// Called when a message is received in a DM channel.
        /// </summary>
        protected abstract Task OnDmMessage(IMessage msg);

        /// <summary>
        /// Called when a message is received in the game's public channel.
        /// </summary>
        protected abstract Task OnPublicMessage(IMessage msg);

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
        /// <param name="endmsg">The message that should be displayed announcing the the end of the game.</param>
        /// <remarks>MUST be called to unsubscribe the message handler.</remarks>
        public virtual async Task EndGame(string endmsg)
        {
            await Task.Run(() => _client.MessageReceived -= ProcessMessage);
        }

        /// <summary>
        /// Get a string that represent the state of the game.
        /// </summary>
        public abstract string GetGameState();
    }
}
