using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Implementation of <see cref="IPermissionConfig"/> using
    /// in-memory collection as a backing store, suitable for
    /// serialization to and from JSON.
    /// </summary>
    public class JsonConfig : IPermissionConfig
    {
        /// <summary>
        /// Gets the ID of the group that is considered
        /// the Admin role in a specified guild.
        /// </summary>
        public Dictionary<ulong, ulong> GuildAdminRole { get; }

        /// <summary>
        /// Gets the ID of the group that is considered
        /// the Moderator role in a specified guild.
        /// </summary>
        public Dictionary<ulong, ulong> GuildModRole { get; }

        /// <summary>
        /// Gets the list of modules that are
        /// whitelisted in a specified channel.
        /// </summary>
        public Dictionary<ulong, HashSet<string>> ChannelModuleWhitelist { get; }

        /// <summary>
        /// Gets the users that are allowed to use
        /// commands marked <see cref="MinimumPermission.Special"/>
        /// in a channel.
        /// </summary>
        public Dictionary<ulong, HashSet<ulong>> SpecialPermissionUsersList { get; }

        async Task IPermissionConfig.AddNewGuild(IGuild guild)
        {
            GuildAdminRole[guild.Id] = 0ul;
            GuildModRole[guild.Id] = 0ul;
            foreach (var channel in await guild.GetTextChannelsAsync())
            {
                ChannelModuleWhitelist[channel.Id] = new HashSet<string>();
                SpecialPermissionUsersList[channel.Id] = new HashSet<ulong>();
            }
        }

        Task IPermissionConfig.AddChannel(IChannel channel)
        {
            ChannelModuleWhitelist[channel.Id] = new HashSet<string>();
            SpecialPermissionUsersList[channel.Id] = new HashSet<ulong>();
            return Task.CompletedTask;
        }

        Task IPermissionConfig.RemoveChannel(ulong channelId)
        {
            ChannelModuleWhitelist.Remove(channelId);
            SpecialPermissionUsersList.Remove(channelId);
            return Task.CompletedTask;
        }

        ulong IPermissionConfig.GetGuildAdminRole(ulong guildId)
        {
            return GuildAdminRole[guildId];
        }

        ulong IPermissionConfig.GetGuildModRole(ulong guildId)
        {
            return GuildModRole[guildId];
        }

        Task<bool> IPermissionConfig.SetGuildAdminRole(ulong guildId, IRole role)
        {
            GuildAdminRole[guildId] = role.Id;
            return Task.FromResult(true);
        }

        Task<bool> IPermissionConfig.SetGuildModRole(ulong guildId, IRole role)
        {
            GuildModRole[guildId] = role.Id;
            return Task.FromResult(true);
        }

        IEnumerable<string> IPermissionConfig.GetChannelModuleWhitelist(ulong channelId)
        {
            return ChannelModuleWhitelist[channelId];
        }

        Task<bool> IPermissionConfig.WhitelistModule(ulong channelId, string moduleName)
        {
            return Task.FromResult(ChannelModuleWhitelist[channelId].Add(moduleName));
        }

        Task<bool> IPermissionConfig.BlacklistModule(ulong channelId, string moduleName)
        {
            
            return Task.FromResult(ChannelModuleWhitelist[channelId].Remove(moduleName));
        }

        IEnumerable<ulong> IPermissionConfig.GetSpecialPermissionUsersList(ulong channelId)
        {
            return SpecialPermissionUsersList[channelId];
        }

        Task<bool> IPermissionConfig.AddSpecialUser(ulong channelId, IUser user)
        {
            
            return Task.FromResult(SpecialPermissionUsersList[channelId].Add(user.Id));
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(ulong channelId, IUser user)
        {
            return Task.FromResult(SpecialPermissionUsersList[channelId].Remove(user.Id));
        }
    }
}
