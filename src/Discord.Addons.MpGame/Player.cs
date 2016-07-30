using System;
using System.Threading.Tasks;

namespace Discord.Addons.MpGame
{
    /// <summary>
    /// Represents a Discord user as a Player
    /// </summary>
    public class Player
    {
        /// <summary>
        /// The underlying <see cref="IGuildUser"/> instance.
        /// </summary>
        public IGuildUser User { get; }

        /// <summary>
        /// The user's DM Channel instance.
        /// </summary>
        internal IDMChannel DmChannel { get; }

        /// <summary>
        /// Creates a <see cref="Player"/> out of an <see cref="IGuildUser"/>.
        /// </summary>
        public Player(IGuildUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            User = user;
            DmChannel = user.CreateDMChannelAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends a message to this <see cref="Player"/>'s DM Channel.
        /// </summary>
        /// <param name="text">The text to send.</param>
        /// <returns>The message that was sent.</returns>
        public async Task<IMessage> SendMessageAsync(string text) => await DmChannel.SendMessageAsync(text);
    }
}
