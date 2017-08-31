using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    public abstract partial class EFBaseConfigContext<TGuild, TChannel, TUser> : IPermissionConfig
        where TGuild : ConfigGuild<TChannel, TUser>, new()
        where TChannel : ConfigChannel<TUser>, new()
        where TUser : ConfigUser, new()
    {
        public CommandService Commands { private get; set; }

        async Task IPermissionConfig.AddNewGuild(IGuild guild)
        {
            var users = await guild.GetUsersAsync(CacheMode.AllowDownload);
            var cUsers = new List<TUser>();
            foreach (var user in users)
            {
                if (!Users.Any(u => u.UserId == user.Id))
                {
                    cUsers.Add(await AddUserInternal(user));
                }
            }

            var tChannels = await guild.GetTextChannelsAsync();
            var cChannels = new List<TChannel>();
            foreach (var chan in tChannels)
            {
                if (!Channels.Any(c => c.ChannelId == chan.Id))
                {
                    cChannels.Add(await AddChannelInternal(chan));
                }
            }

            var cGuild = Guilds.SingleOrDefault(g => g.GuildId == guild.Id);
            if (cGuild == null)
            {
                cGuild = new TGuild
                {
                    GuildId = guild.Id,
                    AdminRole = 0ul,
                    ModRole = 0ul,
                    WhiteListedModules = new List<ConfigModule>(),
                    Users = cUsers,
                    Channels = cChannels
                };
                await OnGuildAdd(cGuild);
                Guilds.Add(cGuild);
            }
            else
            {
                foreach (var user in cUsers)
                {
                    cGuild.Users.Add(user);
                }
                foreach (var chan in cChannels)
                {
                    cGuild.Channels.Add(chan);
                }
            }
        }

        async Task IPermissionConfig.AddChannel(IChannel channel)
        {
            Channels.Add(await AddChannelInternal(channel));
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

        IEnumerable<ModuleInfo> IPermissionConfig.GetChannelModuleWhitelist(IChannel channel)
        {
            var wl = Channels.Include(c => c.WhiteListedModules)
                .Single(c => c.ChannelId == channel.Id)
                .WhiteListedModules.Select(m => m.ModuleName)
                .ToList();
            return _modules.Where(m => wl.Contains(m.Name));
        }

        IEnumerable<ModuleInfo> IPermissionConfig.GetGuildModuleWhitelist(IGuild guild)
        {
            var wl = Guilds.Include(c => c.WhiteListedModules)
                   .Single(g => g.GuildId == guild.Id)
                   .WhiteListedModules.Select(m => m.ModuleName)
                   .ToList();
            return _modules.Where(m => wl.Contains(m.Name));
        }

        Task<bool> IPermissionConfig.WhitelistModule(IChannel channel, string moduleName)
        {
            var chan = Channels.Include(c => c.WhiteListedModules).Single(c => c.ChannelId == channel.Id);
            var mod = _modules.SingleOrDefault(m => m.Name == moduleName);
            if (mod != null)
            {
                var n = Modules.SingleOrDefault(m => m.ModuleName == moduleName);
                if (n == null)
                {
                    n = new ConfigModule { ModuleName = moduleName };
                    Modules.Add(n);
                }
                var hasThis = chan.WhiteListedModules.Any(m => m.Id == n.Id);
                if (!hasThis)
                {
                    chan.WhiteListedModules.Add(n);
                }
                return Task.FromResult(!hasThis);
            }

            return Task.FromResult(false);
        }

        Task<bool> IPermissionConfig.BlacklistModule(IChannel channel, string moduleName)
        {
            var mods = Channels.Include(c => c.WhiteListedModules)
                .Single(c => c.ChannelId == channel.Id).WhiteListedModules;

            bool v = mods.Remove(mods.Single(m => m.ModuleName == moduleName));
            return Task.FromResult(v);
        }

        Task<bool> IPermissionConfig.WhitelistModuleGuild(IGuild guild, string moduleName)
        {
            var gui = Guilds.Include(g => g.WhiteListedModules).Single(g => g.GuildId == guild.Id);
            var mod = _modules.SingleOrDefault(m => m.Name == moduleName);
            if (mod != null)
            {
                var n = Modules.SingleOrDefault(m => m.ModuleName == moduleName);
                if (n == null)
                {
                    n = new ConfigModule { ModuleName = moduleName };
                    Modules.Add(n);
                }
                var hasThis = gui.WhiteListedModules.Any(m => m.Id == n.Id);
                if (!hasThis)
                {
                    gui.WhiteListedModules.Add(n);
                }

                return Task.FromResult(!hasThis);
            }

            return Task.FromResult(false);
        }

        Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, string moduleName)
        {
            var mods = Channels.Include(g => g.WhiteListedModules)
                .Single(g => g.ChannelId == guild.Id).WhiteListedModules;

            bool v = mods.Remove(mods.Single(m => m.ModuleName == moduleName));
            return Task.FromResult(v);
        }

        IEnumerable<ulong> IPermissionConfig.GetSpecialPermissionUsersList(IChannel channel)
        {
            return Channels.Include(c => c.SpecialUsers)
                .Single(c => c.ChannelId == channel.Id)
                .SpecialUsers.Select(u => u.UserId);
        }

        async Task IPermissionConfig.AddUser(IGuildUser user)
        {
            Users.Add(await AddUserInternal(user));
        }

        Task<bool> IPermissionConfig.AddSpecialUser(IChannel channel, IGuildUser user)
        {
            var spUsers = Channels.Include(c => c.SpecialUsers)
                .Single(c => c.ChannelId == channel.Id).SpecialUsers;
            var hasThis = spUsers.Select(u => u.UserId).Contains(user.Id);
            if (!hasThis)
            {
                spUsers.Add(Users.Single(u => u.UserId == user.Id));
            }
            return Task.FromResult(!hasThis);
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(IChannel channel, IGuildUser user)
        {
            var spUsers = Channels.Include(c => c.SpecialUsers)
                .Single(c => c.ChannelId == channel.Id).SpecialUsers;

            bool v = spUsers.Remove(spUsers.Single(u => u.UserId == user.Id));
            return Task.FromResult(v);
        }

        Task IPermissionConfig.SetFancyHelpValue(IGuild guild, bool value)
        {
            Guilds.Single(g => g.GuildId == guild.Id).UseFancyHelp = value;
            return Task.CompletedTask;
        }

        Task<bool> IPermissionConfig.GetFancyHelpValue(IGuild guild)
        {
            return Task.FromResult(Guilds.Single(g => g.GuildId == guild.Id).UseFancyHelp);
        }

        Task IPermissionConfig.SetHidePermCommands(IGuild guild, bool newValue)
        {
            Guilds.Single(g => g.GuildId == guild.Id).HidePermCommands = newValue;
            return Task.CompletedTask;
        }

        Task<bool> IPermissionConfig.GetHidePermCommands(IGuild guild)
        {
            return Task.FromResult(Guilds.Single(g => g.GuildId == guild.Id).HidePermCommands);
        }

        public void Save() => SaveChanges();

        private async Task<TChannel> AddChannelInternal(IChannel channel)
        {
            var cChannel = new TChannel
            {
                ChannelId = channel.Id,
                WhiteListedModules = new List<ConfigModule>(),
                SpecialUsers = new List<TUser>()
            };
            await OnChannelAdd(cChannel);
            return cChannel;
        }

        private async Task<TUser> AddUserInternal(IGuildUser user)
        {
            var cUser = new TUser { UserId = user.Id };
            await OnUserAdd(cUser);
            return cUser;
        }
    }
}
