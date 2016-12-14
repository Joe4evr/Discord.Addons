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

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="modelBuilder"></param>
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //}

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
                        WhiteListedModules = new List<ConfigModule>(),
                        SpecialUsers = new List<ConfigUser>()
                    }).ToList()
            });
        }

        Task IPermissionConfig.AddChannel(IChannel channel)
        {
            Channels.Add(new ConfigChannel
            {
                ChannelId = channel.Id,
                WhiteListedModules = new List<ConfigModule>(),
                SpecialUsers = new List<ConfigUser>()
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

        Task<bool> IPermissionConfig.AddSpecialUser(IChannel channel, IUser user)
        {
            var spUsers = Channels.Include(c => c.SpecialUsers)
                .Single(c => c.ChannelId == channel.Id).SpecialUsers;
            var hasThis = spUsers.Select(u => u.UserId).Contains(user.Id);
            if (!hasThis)
            {
                spUsers.Add(new ConfigUser { UserId = user.Id });
                //SaveChanges();
            }
            return Task.FromResult(!hasThis);
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(IChannel channel, IUser user)
        {
            var spUsers = Channels.Include(c => c.SpecialUsers)
                .Single(c => c.ChannelId == channel.Id).SpecialUsers;
            return Task.FromResult(spUsers.Remove(spUsers.Single(u => u.UserId == user.Id)));
        }
    }
}
