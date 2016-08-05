namespace Discord.Addons.SimpleConfig
{
    /// <summary>
    /// Contract for a basic configuration object.
    /// </summary>
    public interface IConfig
    {
        /// <summary>
        /// Discord ID of the bot owner.
        /// </summary>
        ulong OwnerId { get; }

        /// <summary>
        /// The bot's login token.
        /// </summary>
        string LoginToken { get; }
    }
}
