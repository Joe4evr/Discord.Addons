using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    [Name(permModuleName)]
    public sealed class PermissionsModule : ModuleBase
    {
        internal const string permModuleName = "Permissions";
        private static readonly Type hiddenAttr = typeof(HiddenAttribute);

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
                .Where(c => !c.Preconditions.Any(p => p is HiddenAttribute))
                .GroupBy(c => c.Module.Aliases.FirstOrDefault());

            var sb = new StringBuilder("You can use the following commands:\n")
                .AppendLine($"`{String.Join("`, `", cmds.SelectMany(g => g.Select(c => c.Aliases.FirstOrDefault())).Distinct())}`\n")
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
                .Where(c => c.Aliases.FirstOrDefault().Equals(cmdname, StringComparison.OrdinalIgnoreCase)
                    && !c.Preconditions.Any(p => p is HiddenAttribute));

            if (cmds.Count() > 0)
            {
                sb.AppendLine($"`{cmds.First().Aliases.FirstOrDefault()}`");
                foreach (var cmd in cmds)
                {
                    sb.AppendLine('\t' + cmd.Summary);
                    if (cmd.Parameters.Count > 0)
                        sb.AppendLine($"\t\tParameters: {String.Join(" ", cmd.Parameters.Select(p => formatParam(p)))}");
                }
            }
            else return;

            await ReplyAsync(sb.ToString());
        }

        private string formatParam(Commands.ParameterInfo param)
        {
            var sb = new StringBuilder();
            if (param.IsMultiple)
            {
                sb.Append($"`[({param.Type.Name}): {param.Name}...]`");
            }
            else if (param.IsRemainder) //&& IsOptional - decided not to check for the combination
            {
                sb.Append($"`<({param.Type.Name}): {param.Name}...>`");
            }
            else if (param.IsOptional)
            {
                sb.Append($"`[({param.Type.Name}): {param.Name}]`");
            }
            else
            {
                sb.Append($"`<({param.Type.Name}): {param.Name}>`");
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
                    .Where(m => m.Name != permModuleName)
                    .Select(m => m.Name);
                var index = 1;
                var sb = new StringBuilder("All loaded modules:\n```");
                foreach (var m in mods)
                {
                    sb.AppendLine($"{index,3}: {m}");
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

                await _permService.Config.SetGuildAdminRole(Context.Guild.Id, role);
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

                await _permService.Config.SetGuildModRole(Context.Guild.Id, role);
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
            await _permService.Config.AddSpecialUser(Context.Channel.Id, user);
            _permService.SaveConfig();
            await ReplyAsync($"Gave **{user.Username}** Special command privileges.");
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
            await _permService.Config.RemoveSpecialUser(Context.Channel.Id, user);
            _permService.SaveConfig();
            await ReplyAsync($"Removed **{user.Username}** Special command privileges.");
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
                await _permService.Config.WhitelistModule(ch.Id, mod.Name);
                _permService.SaveConfig();
                await ReplyAsync($"Module `{mod.Name}` is now whitelisted in this channel.");
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
                if (mod.Name == permModuleName)
                {
                    await ReplyAsync($"Not allowed to blacklist {nameof(PermissionsModule)}.");
                }
                else
                {
                    await _permService.Config.BlacklistModule(ch.Id, mod.Name);
                    _permService.SaveConfig();
                    await ReplyAsync($"Module `{mod.Name}` is now blacklisted in this channel.");
                }
            }
        }
    }
}
