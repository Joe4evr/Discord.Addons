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

        Task IPermissionConfig.RemoveChannel(IChannel channel)
        {
            ChannelModuleWhitelist.Remove(channel.Id);
            SpecialPermissionUsersList.Remove(channel.Id);
            return Task.CompletedTask;
        }

        ulong IPermissionConfig.GetGuildAdminRole(IGuild guild)
        {
            return GuildAdminRole[guild.Id];
        }

        ulong IPermissionConfig.GetGuildModRole(IGuild guild)
        {
            return GuildModRole[guild.Id];
        }

        Task<bool> IPermissionConfig.SetGuildAdminRole(IGuild guild, IRole role)
        {
            GuildAdminRole[guild.Id] = role.Id;
            return Task.FromResult(true);
        }

        Task<bool> IPermissionConfig.SetGuildModRole(IGuild guild, IRole role)
        {
            GuildModRole[guild.Id] = role.Id;
            return Task.FromResult(true);
        }

        IEnumerable<string> IPermissionConfig.GetChannelModuleWhitelist(IChannel channel)
        {
            return ChannelModuleWhitelist[channel.Id];
        }

        Task<bool> IPermissionConfig.WhitelistModule(IChannel channel, string moduleName)
        {
            return Task.FromResult(ChannelModuleWhitelist[channel.Id].Add(moduleName));
        }

        Task<bool> IPermissionConfig.BlacklistModule(IChannel channel, string moduleName)
        {
            
            return Task.FromResult(ChannelModuleWhitelist[channel.Id].Remove(moduleName));
        }

        IEnumerable<ulong> IPermissionConfig.GetSpecialPermissionUsersList(IChannel channel)
        {
            return SpecialPermissionUsersList[channel.Id];
        }

        Task<bool> IPermissionConfig.AddSpecialUser(IChannel channel, IUser user)
        {
            
            return Task.FromResult(SpecialPermissionUsersList[channel.Id].Add(user.Id));
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(IChannel channel, IUser user)
        {
            return Task.FromResult(SpecialPermissionUsersList[channel.Id].Remove(user.Id));
        }
    }
}
