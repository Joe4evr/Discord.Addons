using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    [Name(PermModuleName)]
    public sealed class PermissionsModule : ModuleBase<ICommandContext>
    {
        public const string PermModuleName = "Permissions";
        private const string _star = " *";
        private static readonly Regex _dbgRegex = new Regex(@"(?<name>(.*)), Version=(?<version>(.*)), Culture=(?<culture>(.*)), PublicKeyToken=(?<token>(.*))", RegexOptions.Compiled);
        //private static readonly Type hiddenAttr = typeof(HiddenAttribute);

        private readonly PermissionsService _permService;
        private readonly IServiceProvider _services;

        /// <summary> </summary>
        public PermissionsModule(
            PermissionsService permService,
            IServiceProvider services)
        {
            _permService = permService ?? throw new ArgumentNullException(nameof(permService));
            _services = services;
            //_cmdService = permService.CService;
        }

        ///// <summary> Special debug command. </summary>
        //[Command("debug"), Permission(MinimumPermission.BotOwner)]
        //[Alias("dbg"), Hidden]
        //public async Task DebugCmd()
        //{
        //    var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

        //    var asms = (from xa in AppDomain.CurrentDomain.GetAssemblies()
        //                where !xa.IsDynamic
        //                let m = _dbgRegex.Match(xa.FullName)
        //                where m.Success
        //                select new
        //                {
        //                    Name = m.Groups["name"].Value,
        //                    Version = m.Groups["version"].Value
        //                }).Where(s =>
        //    (!(s.Name.StartsWith("System.") || s.Name.StartsWith("Microsoft.") || s.Name == "mscorlib")));

        //    var info = new EmbedBuilder()
        //        .WithAuthor(a => a.WithName("Debug information")
        //            .WithIconUrl(app.IconUrl))
        //        .WithTitle($"{app.Name} - Created: {app.CreatedAt.ToString("d MMM yyyy, HH:mm UTC")}")
        //        .WithDescription($"{app.Description}\nLoaded (non-System) assemblies:")
        //        .AddFieldSequence(asms, (fb, asm) => fb.WithIsInline(true)
        //            .WithName(asm.Name)
        //            .WithValue(asm.Version))
        //        .WithFooter(fb => fb.WithText($"Up for {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToNiceString()}."))
        //        .WithCurrentTimestamp()
        //        .Build();

        //    await ReplyAsync(String.Empty, embed: info).ConfigureAwait(false);
        //}

        [Command("shutdown"), Permission(MinimumPermission.BotOwner)]
        [Alias("kill"), Hidden]
        public Task ShutdownCmd(int code = 0)
        {
            Environment.Exit(code);
            return Task.CompletedTask;
        }

        /// <summary> Display commands you can use. </summary>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        [Summary("Display commands you can use.")]
        public async Task HelpCmd()
        {
            var cmds = (await _permService.CService.Commands
                .Where(c => !c.Attributes.Any(p => p is HiddenAttribute))
                .CheckConditions(Context, _services, _permService).ConfigureAwait(false));

            if (await UseFancy().ConfigureAwait(false))
            {
                //using (var config = _permService.ReadOnlyConfig)
                using (var config = _permService.LoadConfig())
                {
                    if (await config.GetHidePermCommands(Context.Guild).ConfigureAwait(false))
                        cmds = cmds.Where(c => c.Module.Name != PermModuleName);

                    var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                    await _permService.AddNewFancy(await new FancyHelpMessage(Context.Channel, Context.User, cmds.ToList(), app).SendMessage().ConfigureAwait(false));
                }
            }
            else
            {
                var grouped = cmds.GroupBy(c => c.Module.Name)
                    .Select(g => $"{g.Key}:\n\t`{String.Join("`, `", g.Select(c => c.Aliases.FirstOrDefault()).Distinct())}`");

                var sb = new StringBuilder("You can use the following commands:\n")
                    .AppendLine($"{String.Join("\n", grouped)}\n")
                    .Append("You can use `help <command>` for more information on that command.");

                await ReplyAsync(sb.ToString()).ConfigureAwait(false);
            }
        }

        private async Task<bool> UseFancy()
        {
            //using (var config = _permService.ReadOnlyConfig)
            using (var config = _permService.LoadConfig())
            {
                bool fancyEnabled = Context.Guild != null && await config.GetFancyHelpValue(Context.Guild).ConfigureAwait(false);
                var perms = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(Context.Channel as ITextChannel);
                return fancyEnabled && perms.AddReactions && perms.ManageMessages;
            }
        }

        /// <summary> Display how you can use a command. </summary>
        /// <param name="cmdname"></param>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        [Summary("Display how you can use a command.")]
        public async Task HelpCmd(string cmdname)
        {
            var cmds = (await _permService.CService.Commands.CheckConditions(Context, _services, _permService).ConfigureAwait(false))
                .Where(c => c.Aliases.FirstOrDefault().Equals(cmdname, StringComparison.OrdinalIgnoreCase)
                    && !c.Attributes.Any(p => p is HiddenAttribute));

            if (cmds.Any())
            {
                var sb = new StringBuilder($"`{cmds.First().Aliases.FirstOrDefault()}`\n");
                foreach (var cmd in cmds)
                {
                    sb.AppendLine('\t' + cmd.Summary);
                    if (cmd.Parameters.Count > 0)
                        sb.AppendLine($"\t\tParameters: {String.Join(" ", cmd.Parameters.Select(p => p.FormatParam()))}");
                }

                await ReplyAsync(sb.ToString()).ConfigureAwait(false);
            }
        }

        /// <summary> List this server's roles and their ID. </summary>
        [Command("roles"), Permission(MinimumPermission.GuildOwner)]
        [RequireContext(ContextType.Guild)]
        [Summary("List this server's roles and their ID.")]
        public Task ListRoles()
        {
            return ReplyAsync($"This server's roles:```\n{String.Join("\n", Context.Guild.Roles.Where(r => r.Id != Context.Guild.EveryoneRole.Id).Select(r => $"{r.Name} : {r.Id}"))}\n```");
        }

        /// <summary> List all the modules loaded in the bot. </summary>
        [Command("modules"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        [Summary("List all the modules loaded in the bot.")]
        public Task ListModules()
        {
            var mods = _permService.CService.Modules
                .Where(m => m.Name != PermModuleName && !m.IsSubmodule)
                .Select(m => m.Name);
            var index = 1;
            var sb = new StringBuilder("All loaded modules:\n```");
            //using (var config = _permService.ReadOnlyConfig)
            using (var config = _permService.LoadConfig())
            {
                var wl = config.GetChannelModuleWhitelist(Context.Channel as ITextChannel)
                    .Concat(config.GetGuildModuleWhitelist(Context.Guild)).ToList();
                foreach (var m in mods)
                {
                    sb.AppendLine($"{index,3}: {m}{(wl.Any(w => w.Name == m) ? _star : String.Empty)}");
                    index++;
                }
                sb.Append("```");
            }

            return ReplyAsync(sb.ToString());
        }

        /// <summary> Set the admin role for this server. </summary>
        /// <param name="role"></param>
        [Command("setadmin"), Permission(MinimumPermission.GuildOwner)]
        [RequireContext(ContextType.Guild)]
        [Summary("Set the admin role for this server.")]
        public async Task SetAdminRole(IRole role)
        {
            if (role.Id == Context.Guild.EveryoneRole.Id)
            {
                await ReplyAsync($"Not allowed to set `everyone` as the admin role.").ConfigureAwait(false);
                return;
            }

            if (await _permService.SetGuildAdminRole(Context.Guild, role).ConfigureAwait(false))
                await ReplyAsync($"Set **{role.Name}** as the admin role for this server.").ConfigureAwait(false);
        }

        /// <summary> Set the moderator role for this server. </summary>
        /// <param name="role"></param>
        [Command("setmod"), Permission(MinimumPermission.GuildOwner)]
        [RequireContext(ContextType.Guild)]
        [Summary("Set the moderator role for this server.")]
        public async Task SetModRole(IRole role)
        {
            if (role.Id == Context.Guild.EveryoneRole.Id)
            {
                await ReplyAsync($"Not allowed to set `everyone` as the mod role.").ConfigureAwait(false);
                return;
            }

            if (await _permService.SetGuildModRole(Context.Guild, role).ConfigureAwait(false))
                await ReplyAsync($"Set **{role.Name}** as the mod role for this server.").ConfigureAwait(false);
        }

        /// <summary> Give someone special command privileges in this channel. </summary>
        /// <param name="user"></param>
        [Command("addspecial"), Permission(MinimumPermission.ModRole)]
        [Alias("addsp"), RequireContext(ContextType.Guild)]
        [Summary("Give someone special command privileges in this channel.")]
        public async Task AddSpecialUser(IGuildUser user)
        {
            var perms = user.GetPermissions(Context.Channel as ITextChannel);
            if (perms.ReadMessages && perms.SendMessages)
            {
                if (await _permService.AddSpecialUser(Context.Channel as ITextChannel, user).ConfigureAwait(false))
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
            if (await _permService.RemoveSpecialUser(Context.Channel as ITextChannel, user).ConfigureAwait(false))
                await ReplyAsync($"Removed **{user.Username}** Special command privileges.").ConfigureAwait(false);
        }

        /// <summary> Whitelist a module for this channel. </summary>
        /// <param name="modName"></param>
        [Command("whitelist"), Permission(MinimumPermission.ModRole)]
        [Alias("wl"), RequireContext(ContextType.Guild)]
        [Summary("Whitelist a module for this channel or guild.")]
        public async Task WhitelistModule(string modName, [OverrideTypeReader(typeof(GuildSwitchTypeReader))] bool guildwide = false)
        {
            var mod = _permService.CService.Modules.SingleOrDefault(m => m.Name.Equals(modName, StringComparison.OrdinalIgnoreCase));
            if (mod != null)
            {
                if (guildwide)
                {
                    if (await _permService.WhitelistModuleGuild(Context.Guild, mod).ConfigureAwait(false))
                        await ReplyAsync($"Module `{mod.Name}` is now whitelisted in this server.").ConfigureAwait(false);
                }
                else
                {
                    if (await _permService.WhitelistModule(Context.Channel as ITextChannel, mod).ConfigureAwait(false))
                        await ReplyAsync($"Module `{mod.Name}` is now whitelisted in this channel.").ConfigureAwait(false);
                }
            }
        }

        /// <summary> Blacklist a module for this channel. </summary>
        /// <param name="modName"></param>
        [Command("blacklist"), Permission(MinimumPermission.ModRole)]
        [Alias("bl"), RequireContext(ContextType.Guild)]
        [Summary("Blacklist a module for this channel or guild.")]
        public async Task BlacklistModule(string modName, [OverrideTypeReader(typeof(GuildSwitchTypeReader))] bool guildwide = false)
        {
            var mod = _permService.CService.Modules.SingleOrDefault(m => m.Name.Equals(modName, StringComparison.OrdinalIgnoreCase));
            if (mod != null)
            {
                if (mod.Name == PermModuleName)
                {
                    await ReplyAsync($"Not allowed to blacklist {PermModuleName}.").ConfigureAwait(false);
                }
                else
                {
                    if (guildwide)
                    {
                        if (await _permService.BlacklistModuleGuild(Context.Guild, mod).ConfigureAwait(false))
                            await ReplyAsync($"Module `{mod.Name}` is now blacklisted in this server.").ConfigureAwait(false);
                    }
                    else
                    {
                        if (await _permService.BlacklistModule(Context.Channel as ITextChannel, mod).ConfigureAwait(false))
                            await ReplyAsync($"Module `{mod.Name}` is now blacklisted in this channel.").ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
