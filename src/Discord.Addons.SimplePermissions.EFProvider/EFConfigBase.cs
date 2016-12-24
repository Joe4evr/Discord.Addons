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

        /// <summary>
        /// 
        /// </summary>
        public DbSet<ConfigUser> Users { get; set; }

        protected event Func<ConfigGuild, Task> OnGuildAdd;
        protected event Func<ConfigChannel, Task> OnChannelAdd;
        protected event Func<ConfigUser, Task> OnUserAdd;

        async Task IPermissionConfig.AddNewGuild(IGuild guild)
        {
            var users = await guild.GetUsersAsync();
            var cUsers = new List<ConfigUser>();
            foreach (var user in users)
            {
                var cu = new ConfigUser { UserId = user.Id, GuildId = user.GuildId };
                await OnUserAdd?.Invoke(cu);
                cUsers.Add(cu);
            }

            var tChannels = await guild.GetTextChannelsAsync();
            var cChannels = new List<ConfigChannel>();
            foreach (var chan in tChannels)
            {
                var cch = new ConfigChannel
                {
                    ChannelId = chan.Id,
                    WhiteListedModules = new List<ConfigModule>(),
                    SpecialUsers = new List<ConfigUser>()
                };
                await OnChannelAdd?.Invoke(cch);
                cChannels.Add(cch);
            }

            var cGuild = new ConfigGuild
            {
                GuildId = guild.Id,
                AdminRole = 0ul,
                ModRole = 0ul,
                Users = cUsers,
                Channels = cChannels
            };
            await OnGuildAdd?.Invoke(cGuild);
            Guilds.Add(cGuild);
        }

        async Task IPermissionConfig.AddChannel(IChannel channel)
        {
            var cChannel = new ConfigChannel
            {
                ChannelId = channel.Id,
                WhiteListedModules = new List<ConfigModule>(),
                SpecialUsers = new List<ConfigUser>()
            };
            await OnChannelAdd?.Invoke(cChannel);
            Channels.Add(cChannel);
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
            return Channels.Include(c => c.WhiteListedModules)
                .Single(c => c.ChannelId == channel.Id)
                .WhiteListedModules.Select(m => m.ModuleName);
        }

        Task<bool> IPermissionConfig.WhitelistModule(IChannel channel, string moduleName)
        {
            var chan = Channels.Include(c => c.WhiteListedModules).Single(c => c.ChannelId == channel.Id);
            var mods = chan.WhiteListedModules.Select(m => m.ModuleName);
            var hasThis = mods.Contains(moduleName);
            if (!hasThis)
            {
                chan.WhiteListedModules.Add(new ConfigModule { ModuleName = moduleName });
                //SaveChanges();
            }
            return Task.FromResult(!hasThis);
        }

        Task<bool> IPermissionConfig.BlacklistModule(IChannel channel, string moduleName)
        {
            var mods = Channels.Include(c => c.WhiteListedModules)
                .Single(c => c.ChannelId == channel.Id).WhiteListedModules;
            return Task.FromResult(mods.Remove(mods.Single(m => m.ModuleName == moduleName)));
        }

        IEnumerable<ulong> IPermissionConfig.GetSpecialPermissionUsersList(IChannel channel)
        {
            return Channels.Include(c => c.SpecialUsers)
                .Single(c => c.ChannelId == channel.Id)
                .SpecialUsers.Select(u => u.UserId);
        }

        async Task IPermissionConfig.AddUser(IGuildUser user)
        {
            var cUser = new ConfigUser { UserId = user.Id, GuildId = user.GuildId };
            await OnUserAdd?.Invoke(cUser);
            Users.Add(cUser);
        }

        Task<bool> IPermissionConfig.AddSpecialUser(IChannel channel, IGuildUser user)
        {
            var spUsers = Channels.Include(c => c.SpecialUsers)
                .Single(c => c.ChannelId == channel.Id).SpecialUsers;
            var hasThis = spUsers.Select(u => u.UserId).Contains(user.Id);
            if (!hasThis)
            {
                spUsers.Add(Users.Single(u => u.UserId == user.Id));
                //SaveChanges();
            }
            return Task.FromResult(!hasThis);
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(IChannel channel, IGuildUser user)
        {
            var spUsers = Channels.Include(c => c.SpecialUsers)
                .Single(c => c.ChannelId == channel.Id).SpecialUsers;
            return Task.FromResult(spUsers.Remove(spUsers.Single(u => u.UserId == user.Id)));
        }
    }
}
