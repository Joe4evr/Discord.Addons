using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.SimplePermissions.LiteDbProvider;

namespace Discord.Addons.SimplePermissions
{
    public abstract partial class LiteConfigBase<TGuild, TChannel, TUser> : IPermissionConfig
    {
        //writing operations
        async Task IPermissionConfig.AddNewGuild(IGuild guild)
        {
            var users = await guild.GetUsersAsync(CacheMode.AllowDownload);
            var cUsers = new List<TUser>();
            foreach (var user in users)
            {
                if (QueryUser(user) == null)
                {
                    cUsers.Add(await AddUserInternal(user));
                    //SaveChanges();
                }
            }

            var tChannels = await guild.GetTextChannelsAsync();
            var cChannels = new List<TChannel>();
            foreach (var chan in tChannels)
            {
                cChannels.Add(await AddChannelInternal(chan));
            }

            if (QueryGuild(guild) == null)
            {
                Insert(await AddGuildInternal(guild, cChannels));
                //SaveChanges();
            }
        }


        async Task IPermissionConfig.AddChannel(ITextChannel channel)
        {
            Insert(await AddChannelInternal(channel));
            //SaveChanges();
        }

        Task IPermissionConfig.RemoveChannel(ITextChannel channel)
        {
            Delete<TChannel>(c => c.ChannelId == channel.Id);
            return Task.CompletedTask;
        }

        Task<bool> IPermissionConfig.SetGuildAdminRole(IGuild guild, IRole role)
        {
            QueryGuild(guild).AdminRole = role.Id;
            //SaveChanges();
            return Task.FromResult(true);
        }

        Task<bool> IPermissionConfig.SetGuildModRole(IGuild guild, IRole role)
        {
            QueryGuild(guild).ModRole = role.Id;
            //SaveChanges();
            return Task.FromResult(true);
        }

        Task<bool> IPermissionConfig.WhitelistModule(ITextChannel channel, ModuleInfo module)
        {
            var ch = QueryChannel(channel);
            if (ch != null)
            {
                var n = QueryModule(module);
                if (n == null)
                {
                    n = new ConfigModule { ModuleName = module.Name };
                    Insert(n);
                    //SaveChanges();
                }

                var mods = ch.WhiteListedModules;
                var hasThis = mods.Any(m => m.Id == n.Id);
                if (!hasThis)
                {
                    mods.Add(n);
                    //ch.WhiteListedModules.Add(cm);
                    //SaveChanges();
                }

                return Task.FromResult(!hasThis);
            }

            return Task.FromResult(false);
        }

        Task<bool> IPermissionConfig.BlacklistModule(ITextChannel channel, ModuleInfo module)
        {
            var mods = QueryChannel(channel).WhiteListedModules;
            var mod = mods.SingleOrDefault(m => m.ModuleName == module.Name);

            if (mod != null)
            {
                mods.Remove(mod);
                //SaveChanges();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        Task<bool> IPermissionConfig.WhitelistModuleGuild(IGuild guild, ModuleInfo module)
        {
            var gui = QueryGuild(guild);
            if (gui != null)
            {
                var n = QueryModule(module);
                if (n == null)
                {
                    n = new ConfigModule { ModuleName = module.Name };
                    Insert(n);
                    //SaveChanges();
                }

                var mods = gui.WhiteListedModules;
                var hasThis = mods.Any(m => m.Id == n.Id);
                if (!mods.Any(m => m.Id == n.Id))
                {
                    mods.Add(n);
                    //gui.WhiteListedModules.Add(gm);
                    //SaveChanges();
                }

                return Task.FromResult(!hasThis);
            }

            return Task.FromResult(false);
        }

        Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, ModuleInfo module)
        {
            var mods = QueryGuild(guild).WhiteListedModules;
            var mod = mods.SingleOrDefault(m => m.ModuleName == module.Name);

            if (mod != null)
            {
                mods.Remove(mod);
                //SaveChanges();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        async Task IPermissionConfig.AddUser(IGuildUser user)
        {
            Insert(await AddUserInternal(user));
            //SaveChanges();
        }

        async Task<bool> IPermissionConfig.AddSpecialUser(ITextChannel channel, IGuildUser user)
        {
            var ch = QueryChannel(channel);
            var sp = QueryUser(user) ?? await AddUserInternal(user);

            if (ch != null && sp != null)
            {
                ch.SpecialUsers.Add(sp);
                //SaveChanges();
                return true;
            }
            return false;
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(ITextChannel channel, IGuildUser user)
        {
            var specialUsers = QueryChannel(channel).SpecialUsers;
            var cu = specialUsers.SingleOrDefault(s => s.UserId == user.Id);

            if (cu != null)
            {
                specialUsers.Remove(cu);
                //SaveChanges();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        Task IPermissionConfig.SetFancyHelpValue(IGuild guild, bool newValue)
        {
            QueryGuild(guild).UseFancyHelp = newValue;
            //SaveChanges();
            return Task.CompletedTask;
        }

        Task IPermissionConfig.SetHidePermCommands(IGuild guild, bool newValue)
        {
            QueryGuild(guild).HidePermCommands = newValue;
            //SaveChanges();
            return Task.CompletedTask;
        }

        //read-only operations
        IRole IPermissionConfig.GetGuildAdminRole(IGuild guild)
        {
            return guild.GetRole(QueryGuild(guild).AdminRole);
        }

        IRole IPermissionConfig.GetGuildModRole(IGuild guild)
        {
            return guild.GetRole(QueryGuild(guild).ModRole);
        }


        IEnumerable<ModuleInfo> IPermissionConfig.GetChannelModuleWhitelist(ITextChannel channel)
        {
            var wl = QueryChannel(channel).WhiteListedModules;

            return ModuleInfos.Where(m => wl.Any(w => w.ModuleName == m.Key)).Select(m => m.Value);
        }

        IEnumerable<ModuleInfo> IPermissionConfig.GetGuildModuleWhitelist(IGuild guild)
        {
            var wl = QueryGuild(guild).WhiteListedModules;

            return ModuleInfos.Where(m => wl.Any(w => w.ModuleName == m.Key)).Select(m => m.Value);
        }

        IEnumerable<IGuildUser> IPermissionConfig.GetSpecialPermissionUsersList(ITextChannel channel)
        {
            return QueryChannel(channel).SpecialUsers.Select(u => channel.Guild.GetUserAsync(u.UserId).Result);
        }

        Task<bool> IPermissionConfig.GetFancyHelpValue(IGuild guild)
        {
            return Task.FromResult(QueryGuild(guild).UseFancyHelp);
        }

        Task<bool> IPermissionConfig.GetHidePermCommands(IGuild guild)
        {
            return Task.FromResult(QueryGuild(guild).HidePermCommands);
        }


        private async Task<TGuild> AddGuildInternal(
            IGuild guild,
            IList<TChannel> channels)
        {
            var cGuild = new TGuild
            {
                GuildId = guild.Id,
                AdminRole = 0ul,
                ModRole = 0ul,
                Channels = channels,
            };
            await OnGuildAdd(cGuild, guild);
            return cGuild;
        }

        private async Task<TChannel> AddChannelInternal(ITextChannel channel)
        {
            var cChannel = new TChannel
            {
                ChannelId = channel.Id
            };
            await OnChannelAdd(cChannel, channel);
            return cChannel;
        }

        private async Task<TUser> AddUserInternal(IGuildUser user)
        {
            var cUser = new TUser { UserId = user.Id };
            await OnUserAdd(cUser, user);
            return cUser;
        }

        //query helpers
        private TGuild QueryGuild(IGuild guild)
            => Guilds.Where(g => g.GuildId == guild.Id).SingleOrDefault();

        private TChannel QueryChannel(ITextChannel channel)
            => Channels.Where(c => c.ChannelId == channel.Id).SingleOrDefault();

        private TUser QueryUser(IGuildUser user)
            => Users.Where(u => u.UserId == user.Id).SingleOrDefault();

        private ConfigModule QueryModule(ModuleInfo module)
            => Modules.Where(m => m.ModuleName.Equals(module.Name, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();

        //private IQueryable<ChannelUser<TChannel, TUser>> QuerySpecialUsers()
        //    => ChannelUsers.Include(cu => cu.Channel).Include(cu => cu.User);

        //private IQueryable<ChannelModule<TChannel, TUser>> QueryChannelModules()
        //    => ChannelModules.Include(cm => cm.Channel).Include(cm => cm.Module);

        //private IQueryable<GuildModule<TGuild, TChannel, TUser>> QueryGuildModules()
        //    => GuildModules.Include(gm => gm.Guild).Include(gm => gm.Module);
    }
}
