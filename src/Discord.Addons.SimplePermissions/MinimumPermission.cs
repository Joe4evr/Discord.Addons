namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Determines what a command's minimum perission requirement shoulkd be.
    /// </summary>
    public enum MinimumPermission
    {
        /// <summary>
        /// Everyone can use this command.
        /// </summary>
        Everyone = 0,
        /// <summary>
        /// People in the mod role or higher can use this command.
        /// </summary>
        ModRole = 1,
        /// <summary>
        /// People in the admin role or higher can use this command.
        /// </summary>
        AdminRole = 2,
        /// <summary>
        /// The guild owner can use this command.
        /// </summary>
        GuildOwner = 3,
        /// <summary>
        /// Someone is specially allowed to use this command.
        /// </summary>
        Special = 4,
        /// <summary>
        /// Only the bot owner can use this command.
        /// </summary>
        BotOwner = 5
    }
}
