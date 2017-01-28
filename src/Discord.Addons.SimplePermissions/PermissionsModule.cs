using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using CommandParam = Discord.Commands.ParameterInfo;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    [Name(PermModuleName)]
    public sealed class PermissionsModule : ModuleBase<SocketCommandContext>
    {
        public const string PermModuleName = "Permissions";
        private static readonly Regex _dbgRegex = new Regex(@"(?<name>(.*)), Version=(?<version>(.*)), Culture=(?<culture>(.*)), PublicKeyToken=(?<token>(.*))", RegexOptions.Compiled);
        //private static readonly Type hiddenAttr = typeof(HiddenAttribute);

        private readonly PermissionsService _permService;
        private readonly CommandService _cmdService;
        private readonly IDependencyMap _map;

        /// <summary> </summary>
        public PermissionsModule(
            PermissionsService permService,
            IDependencyMap map)
        {
            _permService = permService ?? throw new ArgumentNullException(nameof(permService));
            _cmdService = permService.CService;
            _map = map;
        }

        /// <summary> Special debug command. </summary>
        [Command("debug"), Permission(MinimumPermission.BotOwner)]
        [Alias("dbg"), Hidden]
        public async Task DebugCmd()
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(xa => !xa.IsDynamic);

            var info = new EmbedBuilder()
                .WithAuthor(a => a.WithName("Debug information")
                    .WithIconUrl(app.IconUrl))
                .WithTitle($"{app.Name} - {app.CreatedAt}")
                .WithDescription($"{app.Description}\nLoaded (non-System) assemblies:")
                .AddFieldSequence(assemblies,
                (fb, asm) =>
                {
                    var match = _dbgRegex.Match(asm.FullName);
                    if (match.Success)
                    {
                        var s = new
                        {
                            Name = match.Groups["name"].Value,
                            Version = match.Groups["version"].Value
                        };

                        if (!s.Name.StartsWith("System."))
                        {
                            fb.WithIsInline(true)
                                .WithName(s.Name)
                                .WithValue(s.Version);
                        }
                    }
                })
                .WithFooter(fb => fb.WithText($"Up for {TimeSpan.FromMilliseconds(Environment.TickCount).ToNiceString()}."))
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync("", embed: info).ConfigureAwait(false);
        }

        /// <summary> Display commands you can use or how to use them. </summary>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        [Summary("Display commands you can use.")]
        public async Task HelpCmd()
        {
            var cmds = (await _cmdService.Commands.CheckConditions(Context, _map).ConfigureAwait(false))
                .Where(c => !c.Preconditions.Any(p => p is HiddenAttribute))
                .GroupBy(c => c.Module.Name)
                .Select(g => $"{g.Key}:\n\t`{String.Join("`, `", g.Select(c => c.Aliases.FirstOrDefault()).Distinct())}`");

            var sb = new StringBuilder("You can use the following commands:\n")
                .AppendLine($"{String.Join("\n", cmds)}\n")
                .Append("You can use `help <command>` for more information on that command.");

            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        /// <summary> Display commands you can use or how to use them. </summary>
        /// <param name="cmdname"></param>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        [Summary("Display how you can use a command.")]
        public async Task HelpCmd(string cmdname)
        {
            var sb = new StringBuilder();
            var cmds = (await _cmdService.Commands.CheckConditions(Context, _map).ConfigureAwait(false))
                .Where(c => c.Aliases.FirstOrDefault().Equals(cmdname, StringComparison.OrdinalIgnoreCase)
                    && !c.Preconditions.Any(p => p is HiddenAttribute));

            if (cmds.Any())
            {
                sb.AppendLine($"`{cmds.First().Aliases.FirstOrDefault()}`");
                foreach (var cmd in cmds)
                {
                    sb.AppendLine('\t' + cmd.Summary);
                    if (cmd.Parameters.Count > 0)
                        sb.AppendLine($"\t\tParameters: {String.Join(" ", cmd.Parameters.Select(p => formatParam(p)))}");
                }
            }
            else
            {
                return;
            }

            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        private string formatParam(CommandParam param)
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

        /// <summary> List this server's roles and their ID. </summary>
        [Command("roles"), Permission(MinimumPermission.GuildOwner)]
        [RequireContext(ContextType.Guild)]
        [Summary("List this server's roles and their ID.")]
        public Task ListRoles()
        {
            return (Context.Channel is IGuildChannel ch)
                ? ReplyAsync(
                    $"This server's roles:\n {String.Join("\n", Context.Guild.Roles.Where(r => r.Id != Context.Guild.EveryoneRole.Id).Select(r => $"{r.Name} : {r.Id}"))}")
                : Task.CompletedTask;
        }

        /// <summary> "List all the modules loaded in the bot. </summary>
        [Command("modules"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        [Summary("List all the modules loaded in the bot.")]
        public Task ListModules()
        {
            if (Context.Channel is IGuildChannel ch)
            {
                var mods = _cmdService.Modules
                    .Where(m => m.Name != PermModuleName)
                    .Select(m => m.Name);
                var index = 1;
                var sb = new StringBuilder("All loaded modules:\n```");
                foreach (var m in mods)
                {
                    sb.AppendLine($"{index,3}: {m}");
                    index++;
                }
                sb.Append("```");

                return ReplyAsync(sb.ToString());
            }
            return Task.CompletedTask;
        }

        /// <summary> Set the admin role for this server. </summary>
        /// <param name="role"></param>
        [Command("setadmin"), Permission(MinimumPermission.GuildOwner)]
        [RequireContext(ContextType.Guild)]
        [Summary("Set the admin role for this server.")]
        public async Task SetAdminRole(IRole role)
        {
            if (Context.Channel is IGuildChannel ch)
            {
                if (role.Id == Context.Guild.EveryoneRole.Id)
                {
                    await ReplyAsync($"Not allowed to set `everyone` as the admin role.").ConfigureAwait(false);
                    return;
                }

                if (await _permService.SetGuildAdminRole(Context.Guild, role).ConfigureAwait(false))
                    await ReplyAsync($"Set **{role.Name}** as the admin role for this server.").ConfigureAwait(false);
            }
        }

        /// <summary> Set the moderator role for this server. </summary>
        /// <param name="role"></param>
        [Command("setmod"), Permission(MinimumPermission.GuildOwner)]
        [RequireContext(ContextType.Guild)]
        [Summary("Set the moderator role for this server.")]
        public async Task SetModRole(IRole role)
        {
            if (Context.Channel is IGuildChannel ch)
            {
                if (role.Id == Context.Guild.EveryoneRole.Id)
                {
                    await ReplyAsync($"Not allowed to set `everyone` as the mod role.").ConfigureAwait(false);
                    return;
                }

                if (await _permService.SetGuildModRole(Context.Guild, role).ConfigureAwait(false))
                    await ReplyAsync($"Set **{role.Name}** as the mod role for this server.").ConfigureAwait(false);
            }
        }

        /// <summary> Give someone special command privileges in this channel. </summary>
        /// <param name="user"></param>
        [Command("addspecial"), Permission(MinimumPermission.ModRole)]
        [Alias("addsp"), RequireContext(ContextType.Guild)]
        [Summary("Give someone special command privileges in this channel.")]
        public async Task AddSpecialUser(IGuildUser user)
        {
            if (user.CanReadAndWrite(Context.Channel as ITextChannel))
            {
                if (await _permService.AddSpecialUser(Context.Channel, user).ConfigureAwait(false))
                    await ReplyAsync($"Gave **{user.Username}** Special command privileges.").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("That user has no read/write permissions in this channel.").ConfigureAwait(false);
            }
        }

        /// <summary> Remove someone's special command privileges in this channel. </summary>
        /// <param name="user"></param>
        [Command("remspecial"), Permission(MinimumPermission.ModRole)]
        [Alias("remsp"), RequireContext(ContextType.Guild)]
        [Summary("Remove someone's special command privileges in this channel.")]
        public async Task RemoveSpecialUser(IGuildUser user)
        {
            if (await _permService.RemoveSpecialUser(Context.Channel, user).ConfigureAwait(false))
                await ReplyAsync($"Removed **{user.Username}** Special command privileges.").ConfigureAwait(false);
        }

        /// <summary> Whitelist a module for this channel. </summary>
        /// <param name="modName"></param>
        [Command("whitelist"), Permission(MinimumPermission.ModRole)]
        [Alias("wl"), RequireContext(ContextType.Guild)]
        [Summary("Whitelist a module for this channel.")]
        public async Task WhitelistModule(string modName, [OverrideTypeReader(typeof(SpecialBoolTypeReader))] bool guildwide = false)
        {
            var ch = Context.Channel as IGuildChannel;
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Name == modName);
            if (mod != null)
            {
                if (guildwide)
                {
                    if (await _permService.WhitelistModuleGuild(ch.Guild, mod.Name).ConfigureAwait(false))
                        await ReplyAsync($"Module `{mod.Name}` is now whitelisted in this server.").ConfigureAwait(false);
                }
                else
                {
                    if (await _permService.WhitelistModule(ch, mod.Name).ConfigureAwait(false))
                        await ReplyAsync($"Module `{mod.Name}` is now whitelisted in this channel.").ConfigureAwait(false);
                }
            }
        }

        /// <summary> Blacklist a module for this channel. </summary>
        /// <param name="modName"></param>
        [Command("blacklist"), Permission(MinimumPermission.ModRole)]
        [Alias("bl"), RequireContext(ContextType.Guild)]
        [Summary("Blacklist a module for this channel.")]
        public async Task BlacklistModule(string modName, [OverrideTypeReader(typeof(SpecialBoolTypeReader))] bool guildwide = false)
        {
            var ch = Context.Channel as IGuildChannel;
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Name == modName);
            if (mod != null)
            {
                if (mod.Name == PermModuleName)
                {
                    await ReplyAsync($"Not allowed to blacklist {nameof(PermissionsModule)}.").ConfigureAwait(false);
                }
                else
                {
                    if (guildwide)
                    {
                        if (await _permService.BlacklistModuleGuild(ch.Guild, mod.Name).ConfigureAwait(false))
                            await ReplyAsync($"Module `{mod.Name}` is now blacklisted in this server.").ConfigureAwait(false);
                    }
                    else
                    {
                        if (await _permService.BlacklistModule(ch, mod.Name).ConfigureAwait(false))
                            await ReplyAsync($"Module `{mod.Name}` is now blacklisted in this channel.").ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
