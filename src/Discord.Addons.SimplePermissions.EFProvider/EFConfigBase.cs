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

        Task IPermissionConfig.RemoveChannel(ulong channelId)
        {
            Channels.Remove(Channels.Single(c => c.ChannelId == channelId));
            return Task.CompletedTask;
        }

        ulong IPermissionConfig.GetGuildAdminRole(ulong guildId)
        {
            return Guilds.Single(g => g.GuildId == guildId).AdminRole;
        }

        ulong IPermissionConfig.GetGuildModRole(ulong guildId)
        {
            return Guilds.Single(g => g.GuildId == guildId).ModRole;
        }

        Task<bool> IPermissionConfig.SetGuildAdminRole(ulong guildId, IRole role)
        {
            Guilds.Single(g => g.GuildId == guildId).AdminRole = role.Id;
            return Task.FromResult(true);
        }

        Task<bool> IPermissionConfig.SetGuildModRole(ulong guildId, IRole role)
        {
            Guilds.Single(g => g.GuildId == guildId).ModRole = role.Id;
            return Task.FromResult(true);
        }

        IEnumerable<string> IPermissionConfig.GetChannelModuleWhitelist(ulong channelId)
        {
            return Channels.Single(c => c.ChannelId == channelId).WhiteListedModules;
        }

        Task<bool> IPermissionConfig.WhitelistModule(ulong channelId, string moduleName)
        {
            
            return Task.FromResult(Channels.Single(c => c.ChannelId == channelId).WhiteListedModules.Add(moduleName));
        }

        Task<bool> IPermissionConfig.BlacklistModule(ulong channelId, string moduleName)
        {
            
            return Task.FromResult(Channels.Single(c => c.ChannelId == channelId).WhiteListedModules.Remove(moduleName));
        }

        IEnumerable<ulong> IPermissionConfig.GetSpecialPermissionUsersList(ulong channelId)
        {
            return Channels.Single(c => c.ChannelId == channelId).SpecialUsers;
        }

        Task<bool> IPermissionConfig.AddSpecialUser(ulong channelId, IUser user)
        {
            return Task.FromResult(Channels.Single(c => c.ChannelId == channelId).SpecialUsers.Add(user.Id));
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(ulong channelId, IUser user)
        {
            return Task.FromResult(Channels.Single(c => c.ChannelId == channelId).SpecialUsers.Remove(user.Id));
        }
    }
}
