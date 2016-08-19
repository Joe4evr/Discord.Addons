using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.SimpleConfig;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    [Module]
    public sealed class PermissionsModule/*<TPermissionConfig> where TPermissionConfig : IPermissionConfig*/
    {
        private readonly IConfigStore<IPermissionConfig> _configStore;
        private readonly CommandService _cmdService;
        internal IPermissionConfig Config { get; }
        internal ILookup<string, Command> _commandLookup => _cmdService.Commands.ToLookup(c => c.Name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configstore"></param>
        /// <param name="cmdService"></param>
        public PermissionsModule(IConfigStore<IPermissionConfig> configstore, CommandService cmdService)
        {
            if (configstore == null) throw new ArgumentNullException(nameof(configstore));
            if (cmdService == null) throw new ArgumentNullException(nameof(cmdService));

            Config = configstore.Load();
            _configStore = configstore;
            _cmdService = cmdService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="cmdname"></param>
        /// <returns></returns>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        public async Task HelpCmd(IMessage msg, string cmdname = null)
        {
            var sb = new StringBuilder();
            if (cmdname == null)
            {
                var cmds = _cmdService.Commands
                    .Where(c => c.CheckPreconditions(msg).GetAwaiter().GetResult().IsSuccess)
                    .GroupBy(c => c.Name);

                sb.AppendLine("You can use the following commands:")
                    .AppendLine($"`{String.Join("`, `", cmds.Select(g => g.Select(c => c.Name)).Distinct())}`\n");

            }
            else
            {
                var cmds = _commandLookup[cmdname]
                    .Where(c => c.CheckPreconditions(msg).GetAwaiter().GetResult().IsSuccess);

                if (cmds.Count() > 0)
                {
                    sb.AppendLine(cmds.First().Name);
                    foreach (var cmd in cmds)
                    {
                        sb.AppendLine('\t' + cmd.Summary);
                        sb.AppendLine('\t' + String.Join(" ", cmd.Parameters.Select(p => formatParam(p))));
                    }
                }
                else return;
            }
            await msg.Channel.SendMessageAsync(sb.ToString());
        }

        private string formatParam(CommandParameter param)
        {
            if (param.IsMultiple)
            {
                return String.Concat('[', param.Name, "...]");
            }
            else if (param.IsOptional)
            {
                return String.Concat('[', param.Name, ']');
            }
            else if (param.IsRemainder)
            {
                return String.Concat('<', param.Name, "...>");
            }
            else
            {
                return String.Concat('<', param.Name, '>');
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [Command("roles"), Permission(MinimumPermission.GuildOwner)]
        [Summary("List this server's roles and their ID.")]
        public async Task ListRoles(IMessage msg)
        {
            var ch = msg.Channel as IGuildChannel;
            if (ch != null)
            {
                await msg.Channel.SendMessageAsync(
                    $"This server's roles:\n {String.Join("\n", ch.Guild.Roles.Where(r => r.Id != ch.Guild.EveryoneRole.Id).Select(r => $"{r.Name} : {r.Id}"))}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [Command("modules"), Permission(MinimumPermission.AdminRole)]
        [Summary("List all the modules loaded in the bot.")]
        public async Task ListModules(IMessage msg)
        {
            var ch = msg.Channel as IGuildChannel;
            if (ch != null)
            {
                await msg.Channel.SendMessageAsync(
                    $"Loaded modules:\n {String.Join("\n", _cmdService.Modules.Select(m => m.Name))}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [Command("setadmin"), Permission(MinimumPermission.GuildOwner)]
        [Summary("Set the admin role for this server.")]
        public async Task SetAdminRole(IMessage msg, ulong roleId)
        {
            var ch = msg.Channel as IGuildChannel;
            if (ch != null)
            {
                var role = ch.Guild.GetRole(roleId);
                if (role.Id == ch.Guild.EveryoneRole.Id)
                {
                    await msg.Channel.SendMessageAsync($"Not allowed to set `everyone` as the admin role.");
                    return;
                }

                Config.GuildAdminRole[ch.Guild.Id] = role.Id;
                _configStore.Save(Config);
                await msg.Channel.SendMessageAsync($"Set **{role.Name}** as the admin role for this server.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [Command("setmod"), Permission(MinimumPermission.GuildOwner)]
        [Summary("Set the moderator role for this server.")]
        public async Task SetModRole(IMessage msg, ulong roleId)
        {
            var ch = msg.Channel as IGuildChannel;
            if (ch != null)
            {
                var role = ch.Guild.GetRole(roleId);
                if (role.Id == ch.Guild.EveryoneRole.Id)
                {
                    await msg.Channel.SendMessageAsync($"Not allowed to set `everyone` as the mod role.");
                    return;
                }

                Config.GuildModRole[ch.Guild.Id] = role.Id;
                _configStore.Save(Config);
                await msg.Channel.SendMessageAsync($"Set **{role.Name}** as the mod role for this server.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        [Command("addspecial"), Permission(MinimumPermission.AdminRole)]
        [Summary("Give someone special command priveliges in this channel.")]
        public async Task AddSpecialUser(IMessage msg, IUser user)
        {
            var list = Config.SpecialPermissionUsersList[msg.Channel.Id];
            if (!list.Contains(user.Id))
            {
                list.Add(user.Id);
                await msg.Channel.SendMessageAsync($"Gave **{user.Username}** Special command priveliges.");
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="msg"></param>
        ///// <param name="cmdName"></param>
        ///// <returns></returns>
        //[Command("wl"), Permission(MinimumPermission.AdminRole)]
        //[Description("Whitelist a command for this channel.")]
        //public async Task WhitelistCommand(IMessage msg, string cmdName)
        //{
        //    var ch = msg.Channel as IGuildChannel;
        //    var cmds = _commandLookup[cmdName];
        //    if (cmds.Count() > 1)
        //    {
        //        if (!Config.ChannelCommandWhitelist[ch.Id].Contains(cmdName))
        //        {
        //            Config.ChannelCommandWhitelist[ch.Id].Add(cmdName);
        //            _configStore.Save(Config);
        //            await msg.Channel.SendMessageAsync($"Command `{cmds.First().Name}` is now whitelisted in this channel.");
        //        }
        //    }
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="msg"></param>
        ///// <param name="cmdName"></param>
        ///// <returns></returns>
        //[Command("bl"), Permission(MinimumPermission.AdminRole)]
        //[Description("Blacklist a command for this channel.")]
        //public async Task BlacklistCommand(IMessage msg, string cmdName)
        //{
        //    var ch = msg.Channel as IGuildChannel;
        //    var cmds = _commandLookup[cmdName];
        //    if (cmds.Count() > 1)
        //    {
        //        if (Config.ChannelCommandWhitelist[ch.Id].Contains(cmdName))
        //        {
        //            Config.ChannelCommandWhitelist[ch.Id].Remove(cmdName);
        //            _configStore.Save(Config);
        //            await msg.Channel.SendMessageAsync($"Command `{cmds.First().Name}` is now blacklisted in this channel.");
        //        }
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="modName"></param>
        /// <returns></returns>
        [Command("wl"), Permission(MinimumPermission.AdminRole)]
        [Summary("Whitelist a module for this channel.")]
        public async Task WhitelistModule(IMessage msg, string modName)
        {
            var ch = msg.Channel as IGuildChannel;
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Name == modName);
            if (mod != null)
            {
                if (!Config.ChannelModuleWhitelist[ch.Id].Contains(mod.Name))
                {
                    Config.ChannelModuleWhitelist[ch.Id].Add(mod.Name);
                    _configStore.Save(Config);
                    await msg.Channel.SendMessageAsync($"Module `{mod.Name}` is now whitelisted in this channel.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="modName"></param>
        /// <returns></returns>
        [Command("bl"), Permission(MinimumPermission.AdminRole)]
        [Summary("Blacklist a module for this channel.")]
        public async Task BlacklistModule(IMessage msg, string modName)
        {
            var ch = msg.Channel as IGuildChannel;
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Name == modName);
            if (mod != null)
            {
                if (Config.ChannelModuleWhitelist[ch.Id].Contains(mod.Name))
                {
                    Config.ChannelModuleWhitelist[ch.Id].Remove(mod.Name);
                    _configStore.Save(Config);
                    await msg.Channel.SendMessageAsync($"Module `{mod.Name}` is now blacklisted in this channel.");
                }
            }
        }
    }
}
