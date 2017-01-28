using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store. </summary>
    /// <typeparam name="TGuild">The Guild configuration type.</typeparam>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public abstract class EFBaseConfigContext<TGuild, TChannel, TUser> : DbContext, IPermissionConfig
        where TGuild   : ConfigGuild<TChannel, TUser>, new()
        where TChannel : ConfigChannel<TUser>, new()
        where TUser    : ConfigUser, new()
    {
        /// <summary> </summary>
        public DbSet<TGuild> Guilds { get; set; }

        /// <summary> </summary>
        public DbSet<TChannel> Channels { get; set; }

        /// <summary> </summary>
        public DbSet<TUser> Users { get; set; }

        public DbSet<ConfigModule> Modules { get; set; }

        protected Task OnGuildAdd(TGuild guild)
        {
            guild.WhiteListedModules.Add(Modules.Single(m => m.ModuleName == PermissionsModule.PermModuleName));
            return Task.CompletedTask;
        }
        
        protected Task OnChannelAdd(TChannel channel)
        {
            return Task.CompletedTask;
        }
        
        protected Task OnUserAdd(TUser user)
        {
            return Task.CompletedTask;
        }

        async Task IPermissionConfig.AddNewGuild(IGuild guild)
        {
            var users = await guild.GetUsersAsync();
            var cUsers = new List<TUser>();
            foreach (var user in users)
            {
                var cu = new TUser
                {
                    UserId = user.Id,
                    GuildId = user.GuildId
                };
                await OnUserAdd(cu);
                cUsers.Add(cu);
            }

            var tChannels = await guild.GetTextChannelsAsync();
            var cChannels = new List<TChannel>();
            foreach (var chan in tChannels)
            {
                var cch = new TChannel
                {
                    ChannelId = chan.Id
                };
                await OnChannelAdd(cch);
                cChannels.Add(cch);
            }

            var cGuild = new TGuild
            {
                GuildId = guild.Id,
                AdminRole = 0ul,
                ModRole = 0ul,
                Users = cUsers,
                Channels = cChannels
            };
            await OnGuildAdd(cGuild);
            Guilds.Add(cGuild);
        }

        async Task IPermissionConfig.AddChannel(IChannel channel)
        {
            var cChannel = new TChannel
            {
                ChannelId = channel.Id,
                WhiteListedModules = new List<ConfigModule>(),
                SpecialUsers = new List<TUser>()
            };
            await OnChannelAdd(cChannel);
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

        IEnumerable<string> IPermissionConfig.GetGuildModuleWhitelist(IGuild guild)
        {
            return Guilds.Include(c => c.WhiteListedModules)
                   .Single(g => g.GuildId == guild.Id)
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

        Task<bool> IPermissionConfig.WhitelistModuleGuild(IGuild guild, string moduleName)
        {
            var gui = Guilds.Include(g => g.WhiteListedModules).Single(g => g.GuildId == guild.Id);
            var mods = gui.WhiteListedModules.Select(m => m.ModuleName);
            var hasThis = mods.Contains(moduleName);
            if (!hasThis)
            {
                gui.WhiteListedModules.Add(new ConfigModule { ModuleName = moduleName });
                //SaveChanges();
            }

            return Task.FromResult(!hasThis);
        }

        Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, string moduleName)
        {
            var mods = Channels.Include(g => g.WhiteListedModules)
                .Single(g => g.ChannelId == guild.Id).WhiteListedModules;

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
            var cUser = new TUser { UserId = user.Id, GuildId = user.GuildId };
            await OnUserAdd(cUser);
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

    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store,
    /// using the default Guild type. </summary>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public abstract class EFConfigBaseContext<TChannel, TUser> : EFBaseConfigContext<ConfigGuild<TChannel, TUser>, TChannel, TUser>
        where TChannel : ConfigChannel<TUser>, new()
        where TUser    : ConfigUser, new()
    {
    }

    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store,
    /// using the default Guild and Channel types. </summary>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public abstract class EFConfigBaseContext<TUser> : EFBaseConfigContext<ConfigGuild<TUser>, ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser, new()
    {
    }

    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store,
    /// using the default Guild, Channel and USer types. </summary>
    public abstract class EFConfigBaseContext : EFBaseConfigContext<ConfigGuild, ConfigChannel, ConfigUser>
    {
    }
}
