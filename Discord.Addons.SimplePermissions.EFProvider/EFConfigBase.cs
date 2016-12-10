using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    public class EFConfigBase : DbContext, IPermissionConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public DbSet<ConfigGuild> Guilds { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DbSet<ConfigChannel> Channels { get; set; }

        async Task IPermissionConfig.AddNewGuild(IGuild guild)
        {
            Guilds.Add(new ConfigGuild
            {
                GuildId = guild.Id,
                AdminRole = 0ul,
                ModRole = 0ul,
                Channels = (await guild.GetTextChannelsAsync())
                    .Select(c => new ConfigChannel
                    {
                        ChannelId = c.Id,
                        WhiteListedModules = new HashSet<string>(),
                        SpecialUsers = new HashSet<ulong>()
                    })
            });
        }

        Task IPermissionConfig.AddChannel(IChannel channel)
        {
            Channels.Add(new ConfigChannel
            {
                ChannelId = channel.Id,
                WhiteListedModules = new HashSet<string>(),
                SpecialUsers = new HashSet<ulong>()
            });
            return Task.CompletedTask;
        }

        Task IPermissionConfig.RemoveChannel(IChannel channel)
        {
            Channels.Remove(Channels.Single(c => c.ChannelId == channel.Id));
            return Task.CompletedTask;
        }

        ulong IPermissionConfig.GetGuildAdminRole(IGuild guild)
        {
            return Guilds.Single(g => g.GuildId == guild.Id).AdminRole;
        }

        ulong IPermissionConfig.GetGuildModRole(IGuild guild)
        {
            return Guilds.Single(g => g.GuildId == guild.Id).ModRole;
        }

        Task<bool> IPermissionConfig.SetGuildAdminRole(IGuild guild, IRole role)
        {
            Guilds.Single(g => g.GuildId == guild.Id).AdminRole = role.Id;
            return Task.FromResult(true);
        }

        Task<bool> IPermissionConfig.SetGuildModRole(IGuild guild, IRole role)
        {
            Guilds.Single(g => g.GuildId == guild.Id).ModRole = role.Id;
            return Task.FromResult(true);
        }

        IEnumerable<string> IPermissionConfig.GetChannelModuleWhitelist(IChannel channel)
        {
            return Channels.Single(c => c.ChannelId == channel.Id).WhiteListedModules;
        }

        Task<bool> IPermissionConfig.WhitelistModule(IChannel channel, string moduleName)
        {
            
            return Task.FromResult(Channels.Single(c => c.ChannelId == channel.Id).WhiteListedModules.Add(moduleName));
        }

        Task<bool> IPermissionConfig.BlacklistModule(IChannel channel, string moduleName)
        {
            
            return Task.FromResult(Channels.Single(c => c.ChannelId == channel.Id).WhiteListedModules.Remove(moduleName));
        }

        IEnumerable<ulong> IPermissionConfig.GetSpecialPermissionUsersList(IChannel channel)
        {
            return Channels.Single(c => c.ChannelId == channel.Id).SpecialUsers;
        }

        Task<bool> IPermissionConfig.AddSpecialUser(IChannel channel, IUser user)
        {
            return Task.FromResult(Channels.Single(c => c.ChannelId == channel.Id).SpecialUsers.Add(user.Id));
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(IChannel channel, IUser user)
        {
            return Task.FromResult(Channels.Single(c => c.ChannelId == channel.Id).SpecialUsers.Remove(user.Id));
        }
    }
}
