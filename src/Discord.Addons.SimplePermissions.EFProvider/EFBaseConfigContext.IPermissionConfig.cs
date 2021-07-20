using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord.Commands;
using Discord.Addons.Core;

namespace Discord.Addons.SimplePermissions
{
    public abstract partial class EFBaseConfigContext<TGuild, TChannel, TUser> : IPermissionConfig
    {
        //writing operations
        async Task IPermissionConfig.AddNewGuild(IGuild guild, IReadOnlyCollection<IGuildUser> users)
        {
            foreach (var user in users)
            {
                if (QueryUser(user) == null)
                {
                    Users.Add(await AddUserInternal(user).ConfigureAwait(false));
                }
            }
            await SaveChangesAsync().ConfigureAwait(false);

            var cGuild = QueryGuild(guild);
            if (cGuild == null)
            {
                Guilds.Add(await AddGuildInternal(guild).ConfigureAwait(false));
            }
            else
            {
                var tChannels = await guild.GetTextChannelsAsync().ConfigureAwait(false);
                await AddChannels(cGuild, tChannels).ConfigureAwait(false);
            }
            await SaveChangesAsync().ConfigureAwait(false);
        }

        async Task IPermissionConfig.AddChannel(ITextChannel channel)
        {
            Channels.Add(await AddChannelInternal(channel).ConfigureAwait(false));
            await SaveChangesAsync().ConfigureAwait(false);;
        }

        async Task IPermissionConfig.RemoveChannel(ITextChannel channel)
        {
            Channels.Remove(QueryChannel(channel));
            await SaveChangesAsync().ConfigureAwait(false);
        }

        async Task<bool> IPermissionConfig.SetGuildAdminRole(IRole role)
        {
            var g = QueryGuild(role.Guild);
            if (g != null)
            {
                g.AdminRoleId = role.Id;
                await SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            return false;
        }

        async Task<bool> IPermissionConfig.SetGuildModRole(IRole role)
        {
            var g = QueryGuild(role.Guild);
            if (g != null)
            {
                g.ModRoleId = role.Id;
                await SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            return false;
        }

        async Task<bool> IPermissionConfig.WhitelistModule(ITextChannel channel, ModuleInfo module)
        {
            var ch = QueryChannel(channel);
            if (ch != null)
            {
                var n = QueryModule(module);
                if (n == null)
                {
                    n = new ConfigModule { ModuleName = module.Name };
                    Modules.Add(n);
                    await SaveChangesAsync().ConfigureAwait(false);;
                }

                var hasThis = QueryChannelModules().Any(m => m.Module.Id == n.Id && m.Channel.Id == ch.Id);
                if (!hasThis)
                {
                    var cm = new ChannelModule<TChannel, TUser>
                    {
                        Channel = ch,
                        Module = n
                    };
                    ChannelModules.Add(cm);
                    //ch.WhiteListedModules.Add(cm);
                    await SaveChangesAsync().ConfigureAwait(false);;
                }

                return !hasThis;
            }

            return false;
        }

        async Task<bool> IPermissionConfig.BlacklistModule(ITextChannel channel, ModuleInfo module)
        {
            var mod = await QueryChannelModules()
                .Where(m => m.Channel.ChannelId == channel.Id)
                .SingleOrDefaultAsync(m => m.Module.ModuleName == module.Name).ConfigureAwait(false);

            if (mod != null)
            {
                ChannelModules.Remove(mod);
                await SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            return false;
        }

        async Task<bool> IPermissionConfig.WhitelistModuleGuild(IGuild guild, ModuleInfo module)
        {
            var gui = QueryGuild(guild);
            if (gui != null)
            {
                var n = QueryModule(module);
                if (n == null)
                {
                    n = new ConfigModule { ModuleName = module.Name };
                    Modules.Add(n);
                    await SaveChangesAsync().ConfigureAwait(false);
                }

                var hasThis = QueryGuildModules().Any(m => m.Module.Id == n.Id && m.Guild.Id == gui.Id);
                if (!hasThis)
                {
                    var gm = new GuildModule<TGuild, TChannel, TUser>
                    {
                        Guild = gui,
                        Module = n
                    };
                    GuildModules.Add(gm);
                    //gui.WhiteListedModules.Add(gm);
                    await SaveChangesAsync().ConfigureAwait(false);
                }

                return !hasThis;
            }

            return false;
        }

        async Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, ModuleInfo module)
        {
            var mod = await QueryGuildModules()
                .Where(m => m.Guild.GuildId == guild.Id)
                .SingleOrDefaultAsync(m => m.Module.ModuleName == module.Name).ConfigureAwait(false);

            if (mod != null)
            {
                GuildModules.Remove(mod);
                await SaveChangesAsync().ConfigureAwait(false);;
                return true;
            }
            return false;
        }

        async Task IPermissionConfig.AddUser(IGuildUser user)
        {
            Users.Add(await AddUserInternal(user));
            await SaveChangesAsync().ConfigureAwait(false);;
        }

        async Task<bool> IPermissionConfig.AddSpecialUser(ITextChannel channel, IGuildUser user)
        {
            var cu = QuerySpecialUsers().SingleOrDefault(s => s.Channel.ChannelId == channel.Id && s.User.UserId == user.Id);

            if (cu == null)
            {
                var ch = QueryChannel(channel);
                var sp = QueryUser(user) ?? await AddUserInternal(user).ConfigureAwait(false);

                cu = new ChannelUser<TChannel, TUser>
                {
                    Channel = ch,
                    User = sp
                };
                ChannelUsers.Add(cu);
                await SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            return false;
        }

        async Task<bool> IPermissionConfig.RemoveSpecialUser(ITextChannel channel, IGuildUser user)
        {
            var cu = await QuerySpecialUsers()
                .SingleOrDefaultAsync(u => u.Channel.ChannelId == channel.Id && u.User.UserId == user.Id).ConfigureAwait(false);

            if (cu != null)
            {
                ChannelUsers.Remove(cu);
                await SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            return false;
        }

        async Task IPermissionConfig.SetFancyHelpValue(IGuild guild, bool newValue)
        {
            QueryGuild(guild).UseFancyHelp = newValue;
            await SaveChangesAsync().ConfigureAwait(false);
        }

        async Task IPermissionConfig.SetHidePermCommands(IGuild guild, bool newValue)
        {
            QueryGuild(guild).HidePermCommands = newValue;
            await SaveChangesAsync().ConfigureAwait(false);
        }


        //read-only operations
        IRole IPermissionConfig.GetGuildAdminRole(IGuild guild)
        {
            return guild.GetRole(QueryGuild(guild).AdminRoleId);
        }

        IRole IPermissionConfig.GetGuildModRole(IGuild guild)
        {
            return guild.GetRole(QueryGuild(guild).ModRoleId);
        }

        IEnumerable<ModuleInfo> IPermissionConfig.GetChannelModuleWhitelist(ITextChannel channel)
        {
            var wl = QueryChannelModules()
                .Where(c => c.Channel.ChannelId == channel.Id)
                .Select(m => m.Module.ModuleName)
                .ToList();

            return ModuleInfos.Where(m => wl.Contains(m.Key)).Select(m => m.Value);
        }

        IEnumerable<ModuleInfo> IPermissionConfig.GetGuildModuleWhitelist(IGuild guild)
        {
            var wl = QueryGuildModules()
               .Where(g => g.Guild.GuildId == guild.Id)
               .Select(m => m.Module.ModuleName)
               .ToList();

            return ModuleInfos.Where(m => wl.Contains(m.Key)).Select(m => m.Value);
        }

        IEnumerable<IGuildUser> IPermissionConfig.GetSpecialPermissionUsersList(ITextChannel channel)
        {
            return QuerySpecialUsers()
                .Where(cu => cu.Channel.ChannelId == channel.Id)
                .Select(cu => cu.User.UserId)
                .ToList()
                .Select(id => channel.Guild.GetUserAsync(id).Result);
        }

        Task<bool> IPermissionConfig.GetFancyHelpValue(IGuild guild)
        {
            return Task.FromResult(QueryGuild(guild).UseFancyHelp);
        }

        Task<bool> IPermissionConfig.GetHidePermCommands(IGuild guild)
        {
            return Task.FromResult(QueryGuild(guild).HidePermCommands);
        }


        /// <summary> </summary>
        public void Save() => SaveChanges();


        private async Task<TGuild> AddGuildInternal(IGuild guild)
        {
            var cGuild = new TGuild
            {
                GuildId = guild.Id,
                AdminRoleId = 0ul,
                ModRoleId = 0ul,
                Channels = new List<TChannel>(),
                WhiteListedModules = new List<GuildModule<ConfigGuild<TChannel, TUser>, TChannel, TUser>>(),
                UseFancyHelp = true,
                HidePermCommands = false
            };

            var tChannels = await guild.GetTextChannelsAsync().ConfigureAwait(false);
            await AddChannels(cGuild, tChannels).ConfigureAwait(false);

            await OnGuildAdd(cGuild, guild).ConfigureAwait(false);
            return cGuild;
        }

        private async Task<TChannel> AddChannelInternal(ITextChannel channel)
        {
            var cChannel = new TChannel
            {
                ChannelId = channel.Id,
                SpecialUsers = new List<ChannelUser<ConfigChannel<TUser>, TUser>>(),
                WhiteListedModules = new List<ChannelModule<ConfigChannel<TUser>, TUser>>()
            };
            await OnChannelAdd(cChannel, channel).ConfigureAwait(false);
            return cChannel;
        }

        private async Task<TUser> AddUserInternal(IGuildUser user)
        {
            var cUser = new TUser { UserId = user.Id };
            await OnUserAdd(cUser, user).ConfigureAwait(false);
            return cUser;
        }

        private async Task AddChannels(TGuild cGuild, IReadOnlyCollection<ITextChannel> tChannels)
        {
            foreach (var chan in tChannels)
            {
                if (QueryChannel(chan) == null)
                {
                    cGuild.Channels.Add(await AddChannelInternal(chan).ConfigureAwait(false));
                }
            }
        }


        //query helpers
        private TGuild QueryGuild(IGuild guild)
            => Guilds.SingleOrDefault(g => g.GuildId == guild.Id);

        private TChannel QueryChannel(ITextChannel channel)
            => Channels.SingleOrDefault(c => c.ChannelId == channel.Id);

        private TUser QueryUser(IGuildUser user)
            => Users.SingleOrDefault(u => u.UserId == user.Id);

        private ConfigModule QueryModule(ModuleInfo module)
            => Modules.SingleOrDefault(m => m.ModuleName.Equals(module.Name, StringComparison.OrdinalIgnoreCase));

        private IQueryable<ChannelUser<TChannel, TUser>> QuerySpecialUsers()
            => ChannelUsers.Include(cu => cu.Channel).Include(cu => cu.User);

        private IQueryable<ChannelModule<TChannel, TUser>> QueryChannelModules()
            => ChannelModules.Include(cm => cm.Channel).Include(cm => cm.Module);

        private IQueryable<GuildModule<TGuild, TChannel, TUser>> QueryGuildModules()
            => GuildModules.Include(gm => gm.Guild).Include(gm => gm.Module);
    }
}
