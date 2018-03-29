using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Discord.Net;

namespace Discord.Addons.MpGame
{
    /// <summary> Represents a Discord user as a Player </summary>
    public class Player
    {
        /// <summary> The underlying <see cref="IUser"/> instance. </summary>
        public IUser User { get; }

        private IMessageChannel PubChannel { get; }

        /// <summary> Creates a <see cref="Player"/> out of an <see cref="IUser"/>. </summary>
        /// <param name="user">The user represented.</param>
        /// <param name="channel">The channel where this game is played.</param>
        public Player(IUser user, IMessageChannel channel)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            PubChannel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        private readonly Queue<(string text, Embed embed)> _unsentDms = new Queue<(string, Embed)>();

        /// <summary> Sends a message to this <see cref="Player"/>'s DM Channel
        /// and will cache the message if the user has DMs disabled. </summary>
        /// <param name="text">The text to send.</param>
        public async Task SendMessageAsync(string text, Embed embed = null)
        {
            try
            {
                await User.SendMessageAsync(text, embed: embed).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
            {
                _unsentDms.Enqueue((text, embed));

                if (ShouldKick(_unsentDms.Count))
                {
                    await _rmPlayer(DMsDisabledKickMessage()).ConfigureAwait(false);
                }
                else
                {
                    await PubChannel.SendMessageAsync(DMsDisabledMessage()).ConfigureAwait(false);
                }
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.BadRequest) { }
        }

        internal async Task RetrySendMessageAsync()
        {
            try
            {
                while (_unsentDms.Count > 0)
                {
                    var (t, e) = _unsentDms.Dequeue();
                    await User.SendMessageAsync(t, embed: e).ConfigureAwait(false);
                }
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
            {
                await PubChannel.SendMessageAsync(DMsDisabledMessage()).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.BadRequest) { }
        }

        protected virtual bool ShouldKick(int backstuffedDms) => false;
        protected virtual string DMsDisabledMessage() => $"Player {User.Mention} has their DMs disabled. Please enable DMs and use the resend command if available.";
        protected virtual string DMsDisabledKickMessage() => $"Player {User.Username} has been kicked for having DMs disabled too long.";


        private Func<string, Task> _rmPlayer = _rmnoop;
        internal Func<string, Task> AutoKick { set => _rmPlayer = value; }
        private static readonly Func<string, Task> _rmnoop = (_ => Task.CompletedTask);
    }
}
