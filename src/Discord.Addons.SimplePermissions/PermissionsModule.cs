using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    [Name("Permissions")]
    public sealed class PermissionsModule : ModuleBase
    {
        internal static readonly string permModuleName = typeof(PermissionsModule).FullName;
        private static readonly Type hiddenAttr = typeof(HiddenAttribute);
        //private static readonly TypeInfo permBase = typeof(PermissionModuleBase).GetTypeInfo();

        private readonly PermissionsService _permService;
        private readonly CommandService _cmdService;
        private readonly IDependencyMap _map;

        /// <summary>
        /// 
        /// </summary>
        public PermissionsModule(
            PermissionsService permService,
            CommandService cmdService,
            IDependencyMap map)
        {
            if (permService == null) throw new ArgumentNullException(nameof(permService));
            if (cmdService == null) throw new ArgumentNullException(nameof(cmdService));

            _permService = permService;
            _cmdService = cmdService;
            _map = map;
        }

        /// <summary>
        /// Display commands you can use or how to use them.
        /// </summary>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        [Summary("Display commands you can use.")]
        public async Task HelpCmd()
        {
            var cmds = (await _cmdService.Commands.CheckConditions(Context, _map))
                .Where(c => !c.Source.CustomAttributes.Any(a => a.AttributeType.Equals(hiddenAttr)))
                .GroupBy(c => c.Module.Name);

            var sb = new StringBuilder("You can use the following commands:\n")
                .AppendLine($"`{String.Join("`, `", cmds.SelectMany(g => g.Select(c => c.Text)).Distinct())}`\n")
                .Append("You can use `help <command>` for more information on that command.");

            await ReplyAsync(sb.ToString());
        }

        /// <summary>
        /// Display commands you can use or how to use them.
        /// </summary>
        /// <param name="cmdname"></param>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        [Summary("Display how you can use a command.")]
        public async Task HelpCmd(string cmdname)
        {
            var sb = new StringBuilder();
            var cmds = (await _cmdService.Commands.CheckConditions(Context, _map))
                .Where(c => c.Text.Equals(cmdname, StringComparison.OrdinalIgnoreCase) &&
                    !c.Source.CustomAttributes.Any(a => a.AttributeType.Equals(hiddenAttr)));

            if (cmds.Count() > 0)
            {
                sb.AppendLine($"`{cmds.First().Text}`");
                foreach (var cmd in cmds)
                {
                    sb.AppendLine('\t' + cmd.Summary);
                    if (cmd.Parameters.Count > 0)
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
                var mods = _cmdService.Modules
                    .Where(m => m.Source.FullName != permModuleName)
                    .Select(m => new { m.Name, m.Source.FullName });
                var index = 1;
                var sb = new StringBuilder("All loaded modules:\n```");
                foreach (var m in mods)
                {
                    sb.AppendLine($"{index,3}: {m.Name} ({m.FullName})");
                    index++;
                }
                sb.Append("```");
                    
                await ReplyAsync(sb.ToString());
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
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Source.FullName == modName);
            if (mod != null)
            {
                if (_permService.Config.ChannelModuleWhitelist[ch.Id].Add(mod.Source.FullName))
                {
                    _permService.SaveConfig();
                    //if (permBase.IsAssignableFrom(mod.Source))
                    //{
                    //    mod.Source.InvokeMember
                    //}
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
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Source.FullName == modName);
            if (mod != null)
            {
                if (mod.Source.FullName == permModuleName)
                {
                    await ReplyAsync($"Not allowed to blacklist {nameof(PermissionsModule)}.");
                }
                else if (_permService.Config.ChannelModuleWhitelist[ch.Id].Remove(mod.Source.FullName))
                {
                    _permService.SaveConfig();
                    await ReplyAsync($"Module `{mod.Name}` is now blacklisted in this channel.");
                }
            }
        }
    }
}
