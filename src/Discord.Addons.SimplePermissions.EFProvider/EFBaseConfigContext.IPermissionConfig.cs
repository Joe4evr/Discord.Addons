﻿using System;
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
        async Task IPermissionConfig.AddNewGuild(IGuild guild)
        {
            var users = await guild.GetUsersAsync(CacheMode.AllowDownload);
            var cUsers = new List<TUser>();
            foreach (var user in users)
            {
                if (QueryUser(user) == null)
                {
                    cUsers.Add(await AddUserInternal(user));
                    SaveChanges();
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
                Guilds.Add(await AddGuildInternal(guild, cChannels));
                SaveChanges();
            }
            //else
            //{
            //    foreach (var chan in cChannels)
            //    {
            //        //cGuild.Channels.Add(chan);
            //    }
            //    //SaveChanges();
            //}
        }

        async Task IPermissionConfig.AddChannel(ITextChannel channel)
        {
            Channels.Add(await AddChannelInternal(channel));
            SaveChanges();
        }

        Task IPermissionConfig.RemoveChannel(ITextChannel channel)
        {
            Channels.Remove(QueryChannel(channel));
            return Task.CompletedTask;
        }

        Task<bool> IPermissionConfig.SetGuildAdminRole(IGuild guild, IRole role)
        {
            QueryGuild(guild).AdminRole = role.Id;
            SaveChanges();
            return Task.FromResult(true);
        }

        Task<bool> IPermissionConfig.SetGuildModRole(IGuild guild, IRole role)
        {
            QueryGuild(guild).ModRole = role.Id;
            SaveChanges();
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
                    Modules.Add(n);
                    SaveChanges();
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
                    SaveChanges();
                }

                return Task.FromResult(!hasThis);
            }

            return Task.FromResult(false);
        }

        Task<bool> IPermissionConfig.BlacklistModule(ITextChannel channel, ModuleInfo module)
        {
            var mod = QueryChannelModules()
                .Where(m => m.Channel.ChannelId == channel.Id)
                .SingleOrDefault(m => m.Module.ModuleName == module.Name);

            if (mod != null)
            {
                ChannelModules.Remove(mod);
                SaveChanges();
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
                    Modules.Add(n);
                    SaveChanges();
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
                    SaveChanges();
                }

                return Task.FromResult(!hasThis);
            }

            return Task.FromResult(false);
        }

        Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, ModuleInfo module)
        {
            var mod = QueryGuildModules()
                .Where(m => m.Guild.GuildId == guild.Id)
                .SingleOrDefault(m => m.Module.ModuleName == module.Name);

            if (mod != null)
            {
                GuildModules.Remove(mod);
                SaveChanges();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        async Task IPermissionConfig.AddUser(IGuildUser user)
        {
            Users.Add(await AddUserInternal(user));
            SaveChanges();
        }

        async Task<bool> IPermissionConfig.AddSpecialUser(ITextChannel channel, IGuildUser user)
        {
            var cu = QuerySpecialUsers().SingleOrDefault(s => s.Channel.ChannelId == channel.Id && s.User.UserId == user.Id);

            if (cu == null)
            {
                var ch = QueryChannel(channel);
                var sp = QueryUser(user) ?? await AddUserInternal(user);

                cu = new ChannelUser<TChannel, TUser>
                {
                    Channel = ch,
                    User = sp
                };
                ChannelUsers.Add(cu);
                SaveChanges();
                return true;
            }
            return false;
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(ITextChannel channel, IGuildUser user)
        {
            var cu = QuerySpecialUsers().SingleOrDefault(u => u.Channel.ChannelId == channel.Id && u.User.UserId == user.Id);

            if (cu != null)
            {
                ChannelUsers.Remove(cu);
                SaveChanges();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        Task IPermissionConfig.SetFancyHelpValue(IGuild guild, bool newValue)
        {
            QueryGuild(guild).UseFancyHelp = newValue;
            SaveChanges();
            return Task.CompletedTask;
        }

        Task IPermissionConfig.SetHidePermCommands(IGuild guild, bool newValue)
        {
            QueryGuild(guild).HidePermCommands = newValue;
            SaveChanges();
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


        public void Save() => SaveChanges();


        private async Task<TGuild> AddGuildInternal(
            IGuild guild,
            IEnumerable<TChannel> channels)
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
