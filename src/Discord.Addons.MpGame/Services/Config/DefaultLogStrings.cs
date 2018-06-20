using System;

namespace Discord.Addons.MpGame
{
    //public partial interface ILogStrings
    //{
    internal sealed class DefaultLogStrings : ILogStrings
    {
        public static ILogStrings Instance { get; } = new DefaultLogStrings();
        private DefaultLogStrings() { }

        string ILogStrings.LogRegistration(string gameName)
            => $"Registered service for '{gameName}'";
        string ILogStrings.CleaningGameData(IMessageChannel channel, string gameName)
            => $"Cleaning up '{gameName}' data for channel: #{channel.Id}";
        string ILogStrings.CleaningDMChannel(IDMChannel channel)
            => $"Cleaning up DM channel key: #{channel.Id}";
        string ILogStrings.CleaningGameString(IMessageChannel channel)
            => $"Cleaning up game string for channel: #{channel.Id}";
        string ILogStrings.CreatingGame(IMessageChannel channel, string gameName)
            => $"Creating '{gameName}' data for channel: #{channel.Id}";
        string ILogStrings.PlayerKicked(IUser user)
            => $"Player '{user.Username}' kicked";
        string ILogStrings.SettingGame(IMessageChannel channel, string gameName)
            => $"Setting game '{gameName}' for channel: #{channel.Id}";
        string ILogStrings.RegisteringPlayerTypeReader(string typeName)
            => $"Registering type reader for {typeName}";
        string ILogStrings.DMsDisabledMessage(IUser user)
            => $"Player {user.Mention} has their DMs disabled. Please enable DMs and use the resend command if available.";
        string ILogStrings.DMsDisabledKickMessage(IUser user)
            => $"Player '{user.Username}' has been kicked for having DMs disabled too long.";
    }
    //}
}
