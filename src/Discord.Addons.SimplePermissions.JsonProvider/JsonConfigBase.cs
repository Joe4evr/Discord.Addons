using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of <see cref="IPermissionConfig"/> using
    /// in-memory collection as a backing store, suitable for
    /// serialization to and from JSON. </summary>
    public class JsonConfigBase : IPermissionConfig
    {
        /// <summary> Gets the ID of the group that is considered
        /// the Admin role in a specified guild. </summary>
        public Dictionary<ulong, ulong> GuildAdminRole { get; set; }

        /// <summary> Gets the ID of the group that is considered
        /// the Moderator role in a specified guild. </summary>
        public Dictionary<ulong, ulong> GuildModRole { get; set; }

        /// <summary> Gets the list of modules that are
        /// whitelisted in a specified channel. </summary>
        public Dictionary<ulong, HashSet<string>> ChannelModuleWhitelist { get; set; }

        /// <summary> Gets the list of modules that are
        /// whitelisted in a specified guild. </summary>
        public Dictionary<ulong, HashSet<string>> GuildModuleWhitelist { get; set; }

        /// <summary> Gets the users that are allowed to use
        /// commands marked <see cref="MinimumPermission.Special"/>
        /// in a channel. </summary>
        public Dictionary<ulong, HashSet<ulong>> SpecialPermissionUsersList { get; set; }

        async Task IPermissionConfig.AddNewGuild(IGuild guild)
        {
            if (!GuildAdminRole.ContainsKey(guild.Id))
            {
                GuildAdminRole[guild.Id] = 0ul;
            }
            if (!GuildModRole.ContainsKey(guild.Id))
            {
                GuildModRole[guild.Id] = 0ul;
            }
            if (!GuildModuleWhitelist.ContainsKey(guild.Id))
            {
                GuildModuleWhitelist[guild.Id] = new HashSet<string>();
            }

            foreach (var channel in await guild.GetTextChannelsAsync())
            {
                if (!ChannelModuleWhitelist.ContainsKey(channel.Id))
                {
                    ChannelModuleWhitelist[channel.Id] = new HashSet<string>();
                }
                if (!SpecialPermissionUsersList.ContainsKey(channel.Id))
                {
                    SpecialPermissionUsersList[channel.Id] = new HashSet<ulong>();
                }
            }
            await (this as IPermissionConfig).WhitelistModuleGuild(guild, PermissionsModule.PermModuleName).ConfigureAwait(false);
        }

        Task IPermissionConfig.AddChannel(IChannel channel)
        {
            if (!ChannelModuleWhitelist.ContainsKey(channel.Id))
            {
                ChannelModuleWhitelist[channel.Id] = new HashSet<string>();
            }
            if (!SpecialPermissionUsersList.ContainsKey(channel.Id))
            {
                SpecialPermissionUsersList[channel.Id] = new HashSet<ulong>();
            }
            return Task.CompletedTask;
        }

        Task IPermissionConfig.RemoveChannel(IChannel channel)
        {
            if (ChannelModuleWhitelist.ContainsKey(channel.Id))
            {
                ChannelModuleWhitelist.Remove(channel.Id);
            }
            if (SpecialPermissionUsersList.ContainsKey(channel.Id))
            {
                SpecialPermissionUsersList.Remove(channel.Id);
            }

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

        IEnumerable<string> IPermissionConfig.GetGuildModuleWhitelist(IGuild guild)
        {
            return GuildModuleWhitelist[guild.Id];
        }

        Task<bool> IPermissionConfig.WhitelistModule(IChannel channel, string moduleName)
        {
            return Task.FromResult(ChannelModuleWhitelist[channel.Id].Add(moduleName));
        }

        Task<bool> IPermissionConfig.BlacklistModule(IChannel channel, string moduleName)
        {
            return Task.FromResult(ChannelModuleWhitelist[channel.Id].Remove(moduleName));
        }

        Task<bool> IPermissionConfig.WhitelistModuleGuild(IGuild guild, string moduleName)
        {
            return Task.FromResult(GuildModuleWhitelist[guild.Id].Add(moduleName));
        }

        Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, string moduleName)
        {
            return Task.FromResult(GuildModuleWhitelist[guild.Id].Remove(moduleName));
        }

        IEnumerable<ulong> IPermissionConfig.GetSpecialPermissionUsersList(IChannel channel)
        {
            return SpecialPermissionUsersList[channel.Id];
        }

        Task IPermissionConfig.AddUser(IGuildUser user)
        {
            return Task.CompletedTask;
        }

        Task<bool> IPermissionConfig.AddSpecialUser(IChannel channel, IGuildUser user)
        {

            return Task.FromResult(SpecialPermissionUsersList[channel.Id].Add(user.Id));
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(IChannel channel, IGuildUser user)
        {
            return Task.FromResult(SpecialPermissionUsersList[channel.Id].Remove(user.Id));
        }
    }
}
