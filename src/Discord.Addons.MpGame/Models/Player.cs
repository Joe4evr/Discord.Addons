using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Discord.Net;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame
{
    /// <summary> Represents a Discord user as a Player </summary>
    public class Player
    {
        private readonly Queue<(string text, Embed embed)> _unsentDms = new Queue<(string, Embed)>();

        /// <summary> Creates a <see cref="Player"/> out of an <see cref="IUser"/>. </summary>
        /// <param name="user">The user represented.</param>
        /// <param name="channel">The channel where this game is played.</param>
        public Player(IUser user, IMessageChannel channel)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            PubChannel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        /// <summary> The underlying <see cref="IUser"/> instance. </summary>
        public IUser User { get; }

        private IMessageChannel PubChannel { get; }

        internal string DMsDisabledMessage           { private get; set; } = String.Empty;
        internal string DMsDisabledKickMessage       { private get; set; } = String.Empty;
        internal Func<string, Task> AutoKickCallback { private get; set; } = Extensions.NoOpStringToTask;

        /// <summary> Sends a message to this <see cref="Player"/>'s DM Channel
        /// and will cache the message if the user has DMs disabled. </summary>
        /// <param name="text">The text to send.</param>
        /// <returns>The message that is sent, or
        /// <see langword="null"/> if it couldn't be sent.</returns>
        public async Task<IUserMessage> SendMessageAsync(string text, Embed embed = null)
        {
            try
            {
                return await User.SendMessageAsync(text, embed: embed).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
            {
                _unsentDms.Enqueue((text, embed));

                if (ShouldKick(_unsentDms.Count))
                {
                    if (!String.IsNullOrWhiteSpace(DMsDisabledKickMessage))
                        await AutoKickCallback(DMsDisabledKickMessage).ConfigureAwait(false);
                }
                else
                {
                    if (!String.IsNullOrWhiteSpace(DMsDisabledMessage))
                        await PubChannel.SendMessageAsync(DMsDisabledMessage).ConfigureAwait(false);
                }
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.BadRequest) { }
            return null;
        }

        /// <summary> Can be overriden to determine if
        /// a player should be kicked for not
        /// having DMs enabled for too long. </summary>
        /// <param name="backstuffedDms">The amount of DMs that are
        /// currently not sent to this player.</param>
        /// <returns><see langword="true"/> if the player should be kicked,
        /// otherwise <see langword="false"/>.</returns>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>The default implementation always
        /// returns <see langword="false"/>.</p></div></div></remarks>
        protected virtual bool ShouldKick(int backstuffedDms) => false;

        internal async Task RetrySendMessagesAsync()
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
                if (!String.IsNullOrWhiteSpace(DMsDisabledMessage))
                    await PubChannel.SendMessageAsync(DMsDisabledMessage).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.BadRequest) { }
        }
    }
}
