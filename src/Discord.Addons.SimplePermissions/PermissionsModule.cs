using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    public class PermissionsModule : ModuleBase
    {
        private readonly PermissionsService _permService;
        private readonly CommandService _cmdService;

        /// <summary>
        /// 
        /// </summary>
        public PermissionsModule(PermissionsService permService, CommandService cmdService)
        {
            if (permService == null) throw new ArgumentNullException(nameof(permService));
            if (cmdService == null) throw new ArgumentNullException(nameof(cmdService));

            _permService = permService;
            _cmdService = cmdService;
        }

        /// <summary>
        /// Display commands you can use or how to use them.
        /// </summary>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        [Summary("Display commands you can use or how to use them.")]
        public async Task HelpCmd()
        {
            var sb = new StringBuilder();
            var cmds = (await _cmdService.Commands.CheckConditions(Context))
                .Where(c => !c.Source.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(HiddenAttribute))))
                .GroupBy(c => c.Name);

            sb.AppendLine("You can use the following commands:")
                .AppendLine($"`{String.Join("`, `", cmds.SelectMany(g => g.Select(c => c.Name)).Distinct())}`\n")
                .Append("You can use `help <command>` for more information on that command.");

            await ReplyAsync(sb.ToString());
        }

        /// <summary>
        /// Display commands you can use or how to use them.
        /// </summary>
        /// <param name="cmdname"></param>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        [Summary("Display commands you can use or how to use them.")]
        public async Task HelpCmd(string cmdname)
        {
            var sb = new StringBuilder();
            var cmds = (await _cmdService.Commands.CheckConditions(Context))
                .Where(c => c.Name.Equals(cmdname, StringComparison.OrdinalIgnoreCase) &&
                    !c.Source.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(HiddenAttribute))));

            if (cmds.Count() > 0)
            {
                sb.AppendLine(cmds.First().Name);
                foreach (var cmd in cmds)
                {
                    sb.AppendLine('\t' + cmd.Summary);
                    sb.AppendLine($"\t\t{String.Join(" ", cmd.Parameters.Select(p => formatParam(p)))}");
                }
            }
            else return;

            await ReplyAsync(sb.ToString());
        }

        private string formatParam(CommandParameter param)
        {
            var sb = new StringBuilder();
            if (param.IsMultiple)
            {
                sb.Append($"`[({param.ElementType.Name}): {param.Name}...]`");
            }
            else if (param.IsRemainder) //&& IsOptional - decided not to check for the combination
            {
                sb.Append($"`<({param.ElementType.Name}): {param.Name}...>`");
            }
            else if (param.IsOptional)
            {
                sb.Append($"`[({param.ElementType.Name}): {param.Name}]`");
            }
            else
            {
                sb.Append($"`<({param.ElementType.Name}): {param.Name}>`");
            }

            if (!String.IsNullOrWhiteSpace(param.Summary))
            {
                sb.Append($" ({param.Summary})");
            }
            return sb.ToString();
        }

        /// <summary>
        /// List this server's roles and their ID.
        /// </summary>
        [Command("roles"), Permission(MinimumPermission.GuildOwner)]
        [RequireContext(ContextType.Guild)]
        [Summary("List this server's roles and their ID.")]
        public async Task ListRoles()
        {
            var ch = Context.Channel as IGuildChannel;
            if (ch != null)
            {
                await ReplyAsync(
                    $"This server's roles:\n {String.Join("\n", Context.Guild.Roles.Where(r => r.Id != Context.Guild.EveryoneRole.Id).Select(r => $"{r.Name} : {r.Id}"))}");
            }
        }

        /// <summary>
        /// "List all the modules loaded in the bot.
        /// </summary>
        [Command("modules"), Permission(MinimumPermission.AdminRole)]
        [RequireContext(ContextType.Guild)]
        [Summary("List all the modules loaded in the bot.")]
        public async Task ListModules()
        {
            var ch = Context.Channel as IGuildChannel;
            if (ch != null)
            {
                await ReplyAsync($"All loaded modules:\n {String.Join("\n", _cmdService.Modules.Select(m => m.Name))}");
            }
        }

        /// <summary>
        /// Set the admin role for this server.
        /// </summary>
        /// <param name="role"></param>
        [Command("setadmin"), Permission(MinimumPermission.GuildOwner)]
        [RequireContext(ContextType.Guild)]
        [Summary("Set the admin role for this server.")]
        public async Task SetAdminRole(IRole role)
        {
            var ch = Context.Channel as IGuildChannel;
            if (ch != null)
            {
                if (role.Id == Context.Guild.EveryoneRole.Id)
                {
                    await ReplyAsync($"Not allowed to set `everyone` as the admin role.");
                    return;
                }

                _permService.Config.GuildAdminRole[Context.Guild.Id] = role.Id;
                _permService.SaveConfig();
                await ReplyAsync($"Set **{role.Name}** as the admin role for this server.");
            }
        }

        /// <summary>
        /// Set the moderator role for this server.
        /// </summary>
        /// <param name="role"></param>
        [Command("setmod"), Permission(MinimumPermission.GuildOwner)]
        [RequireContext(ContextType.Guild)]
        [Summary("Set the moderator role for this server.")]
        public async Task SetModRole(IRole role)
        {
            var ch = Context.Channel as IGuildChannel;
            if (ch != null)
            {
                if (role.Id == Context.Guild.EveryoneRole.Id)
                {
                    await ReplyAsync($"Not allowed to set `everyone` as the mod role.");
                    return;
                }

                _permService.Config.GuildModRole[Context.Guild.Id] = role.Id;
                _permService.SaveConfig();
                await ReplyAsync($"Set **{role.Name}** as the mod role for this server.");
            }
        }

        /// <summary>
        /// Give someone special command privileges in this channel.
        /// </summary>
        /// <param name="user"></param>
        [Command("addspecial"), Permission(MinimumPermission.AdminRole)]
        [Alias("addsp"), RequireContext(ContextType.Guild)]
        [Summary("Give someone special command privileges in this channel.")]
        public async Task AddSpecialUser(IUser user)
        {
            var list = _permService.Config.SpecialPermissionUsersList[Context.Channel.Id];
            if (list.Add(user.Id))
            {
                _permService.SaveConfig();
                await ReplyAsync($"Gave **{user.Username}** Special command privileges.");
            }
        }


        /// <summary>
        /// Remove someone's special command privileges in this channel.
        /// </summary>
        /// <param name="user"></param>
        [Command("remspecial"), Permission(MinimumPermission.AdminRole)]
        [Alias("remsp"), RequireContext(ContextType.Guild)]
        [Summary("Remove someone's special command privileges in this channel.")]
        public async Task RemoveSpecialUser(IUser user)
        {
            var list = _permService.Config.SpecialPermissionUsersList[Context.Channel.Id];
            if (list.Remove(user.Id))
            {
                _permService.SaveConfig();
                await ReplyAsync($"Removed **{user.Username}** Special command privileges.");
            }
        }

        /// <summary>
        /// Whitelist a module for this channel.
        /// </summary>
        /// <param name="modName"></param>
        [Command("whitelist"), Permission(MinimumPermission.AdminRole)]
        [Alias("wl"), RequireContext(ContextType.Guild)]
        [Summary("Whitelist a module for this channel.")]
        public async Task WhitelistModule(string modName)
        {
            var ch = Context.Channel as IGuildChannel;
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Name == modName);
            if (mod != null)
            {
                if (_permService.Config.ChannelModuleWhitelist[ch.Id].Add(mod.Name))
                {
                    _permService.SaveConfig();
                    await ReplyAsync($"Module `{mod.Name}` is now whitelisted in this channel.");
                }
            }
        }

        /// <summary>
        /// Blacklist a module for this channel.
        /// </summary>
        /// <param name="modName"></param>
        [Command("blacklist"), Permission(MinimumPermission.AdminRole)]
        [Alias("bl"), RequireContext(ContextType.Guild)]
        [Summary("Blacklist a module for this channel.")]
        public async Task BlacklistModule(string modName)
        {
            var ch = Context.Channel as IGuildChannel;
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Name == modName);
            if (mod != null)
            {
                if (_permService.Config.ChannelModuleWhitelist[ch.Id].Remove(mod.Name))
                {
                    _permService.SaveConfig();
                    await ReplyAsync($"Module `{mod.Name}` is now blacklisted in this channel.");
                }
            }
        }
    }

    internal static class Ext
    {
        public static async Task<IEnumerable<CommandInfo>> CheckConditions(
            this IEnumerable<CommandInfo> commands, CommandContext ctx)
        {
            var ret = new List<CommandInfo>();
            foreach (var cmd in commands)
            {
                if ((await cmd.CheckPreconditions(ctx)).IsSuccess)
                {
                    ret.Add(cmd);
                }
            }
            return ret;
        }
    }
}
