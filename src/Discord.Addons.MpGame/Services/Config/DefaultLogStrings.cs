using System;

namespace Discord.Addons.MpGame
{
    public partial interface ILogStrings
    {
#if NETCOREAPP3_0
        /// <summary>
        ///     The default set of log strings if none specified.
        /// </summary>
        public static ILogStrings Default { get; } = new DefaultLogStrings();

        private sealed class DefaultLogStrings : ILogStrings
        {
#else
        internal sealed class DefaultLogStrings : ILogStrings
        {
            public static ILogStrings Instance { get; } = new DefaultLogStrings();

            private DefaultLogStrings() { }
#endif
            /// <inheritdoc/>
            string ILogStrings.LogRegistration(string gameName)
                => $"Registered service for '{gameName}'";
            /// <inheritdoc/>
            string ILogStrings.CleaningGameData(IMessageChannel channel, string gameName)
                => $"Cleaning up '{gameName}' data for channel: #{channel.Id}";
            /// <inheritdoc/>
            string ILogStrings.CleaningDMChannel(IDMChannel channel)
                => $"Cleaning up DM channel key: #{channel.Id}";
            /// <inheritdoc/>
            string ILogStrings.CleaningGameString(IMessageChannel channel)
                => $"Cleaning up game string for channel: #{channel.Id}";
            /// <inheritdoc/>
            string ILogStrings.CreatingGame(IMessageChannel channel, string gameName)
                => $"Creating '{gameName}' data for channel: #{channel.Id}";
            /// <inheritdoc/>
            string ILogStrings.PlayerKicked(IUser user)
                => $"Player '{user.Username}' kicked";
            /// <inheritdoc/>
            string ILogStrings.SettingGame(IMessageChannel channel, string gameName)
                => $"Setting game '{gameName}' for channel: #{channel.Id}";
            /// <inheritdoc/>
            string ILogStrings.RegisteringPlayerTypeReader(string typeName)
                => $"Registering type reader for {typeName}";
            /// <inheritdoc/>
            string ILogStrings.DMsDisabledMessage(IUser user)
                => $"Player {user.Mention} has their DMs disabled. Please enable DMs and use the resend command if available.";
            /// <inheritdoc/>
            string ILogStrings.DMsDisabledKickMessage(IUser user)
                => $"Player '{user.Username}' has been kicked for having DMs disabled too long.";
        }
    }
}
