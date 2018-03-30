using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons.MpGame
{
    internal class DefaultStrings : IServiceStrings
    {
        public static DefaultStrings Instance { get; } = new DefaultStrings();
        private DefaultStrings() { }

        string IServiceStrings.LogRegistration(string gameName) => $"Registered service for '{gameName}'";
        string IServiceStrings.CleaningGameData(string gameName, IMessageChannel channel) => $"Cleaning up '{gameName}' data for channel: #{channel.Id}";
        string IServiceStrings.CleaningDMChannel(IDMChannel channel) => $"Cleaning up DM channel key: #{channel.Id}";
        string IServiceStrings.CleaningGameString(IMessageChannel channel) => $"Cleaning up game string for channel: #{channel.Id}";
        string IServiceStrings.CreatingGame(string gameName, IMessageChannel channel) => $"Creating '{gameName}' data for channel: #{channel.Id}";
        string IServiceStrings.PlayerKicked(IUser user) => $"Player '{user.Username}' kicked";
        string IServiceStrings.SettingGame(string gameName, IMessageChannel channel) => $"Setting game '{gameName}' for channel: #{channel.Id}";
    }
}
