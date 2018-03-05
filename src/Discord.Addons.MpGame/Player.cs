using System;
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

            //try
            //{
            //    DmChannel = user.GetOrCreateDMChannelAsync().GetAwaiter().GetResult();
            //}
            //catch (HttpException e) when (e.HttpCode == HttpStatusCode.BadRequest) { }
        }

        private string _unsentDm;

        /// <summary> Sends a message to this <see cref="Player"/>'s DM Channel
        /// and will cache the message if the user has DMs disabled. </summary>
        /// <param name="text">The text to send.</param>
        public async Task SendMessageAsync(string text)
        {
            try
            {
                if (text != null)
                {
                    await User.SendMessageAsync(text).ConfigureAwait(false);
                }
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
            {
                if (text == null)
                    _unsentDm = text;

                await PubChannel.SendMessageAsync($"Player {User.Mention} has their DMs disabled. Please enable DMs.").ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.BadRequest) { }
        }

        internal async Task RetrySendMessageAsync()
        {
            try
            {
                if (_unsentDm != null)
                {
                    await User.SendMessageAsync(_unsentDm).ConfigureAwait(false);
                    _unsentDm = null;
                }
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
            {
                await PubChannel.SendMessageAsync($"Player {User.Mention} has their DMs disabled. Please enable DMs.").ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.BadRequest) { }
        }
    }
}
