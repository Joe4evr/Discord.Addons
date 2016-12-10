using System;
using System.Net;
using System.Threading.Tasks;
using Discord.Net;

namespace Discord.Addons.MpGame
{
    /// <summary>
    /// Represents a Discord user as a Player
    /// </summary>
    public class Player
    {
        /// <summary>
        /// The underlying <see cref="IUser"/> instance.
        /// </summary>
        public IUser User { get; }

        /// <summary>
        /// The user's DM Channel instance.
        /// </summary>
        internal IDMChannel DmChannel { get; }

        private readonly IMessageChannel pubChannel;

        /// <summary>
        /// Creates a <see cref="Player"/> out of an <see cref="IUser"/>.
        /// </summary>
        /// <param name="user">The user represented.</param>
        /// <param name="channel">The channel where this game is played.</param>
        public Player(IUser user, IMessageChannel channel)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (channel == null) throw new ArgumentNullException(nameof(channel));

            User = user;

            try
            {
                DmChannel = user.CreateDMChannelAsync().GetAwaiter().GetResult();
            }
            catch (HttpException e) when (e.StatusCode == HttpStatusCode.BadRequest) { }

            pubChannel = channel;
        }

        private string unsentDm;

        /// <summary>
        /// Sends a message to this <see cref="Player"/>'s DM Channel
        /// and will cache the message if the user has DMs disabled.
        /// </summary>
        /// <param name="text">The text to send.</param>
        public async Task SendMessageAsync(string text)
        {
            if (DmChannel != null)
            {
                try
                {
                    if (text != null && unsentDm == null)
                    {
                        await DmChannel.SendMessageAsync(text);
                    }
                }
                catch (HttpException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
                {
                    if (text != null)
                        unsentDm = text;

                    await pubChannel.SendMessageAsync($"Player {User.Mention} has their DMs disabled. Please enable DMs and use `resend` to obtain this info.");
                }
            }
        }

        internal async Task RetrySendMessageAsync()
        {
            try
            {
                if (unsentDm != null)
                {
                    await User.CreateDMChannelAsync();
                    await DmChannel.SendMessageAsync(unsentDm);
                    unsentDm = null;
                }
            }
            catch (HttpException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                await pubChannel.SendMessageAsync($"Player {User.Mention} has their DMs disabled. Please enable DMs and use `resend` to obtain this info.");
            }
        }
    }
}
