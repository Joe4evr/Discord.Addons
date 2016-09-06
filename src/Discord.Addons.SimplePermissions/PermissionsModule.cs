using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.SimpleConfig;
using Discord.WebSocket;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    [Module]
    public sealed class PermissionsModule
    {
        private readonly IConfigStore<IPermissionConfig> _configStore;
        private readonly CommandService _cmdService;
        internal IPermissionConfig Config { get; }
        private ILookup<string, Command> _commandLookup => _cmdService.Commands.ToLookup(c => c.Name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configstore"></param>
        /// <param name="cmdService"></param>
        /// <param name="client"></param>
        public PermissionsModule(
            IConfigStore<IPermissionConfig> configstore,
            CommandService cmdService,
            DiscordSocketClient client)
        {
            if (configstore == null) throw new ArgumentNullException(nameof(configstore));
            if (cmdService == null) throw new ArgumentNullException(nameof(cmdService));
            if (client == null) throw new ArgumentNullException(nameof(client));

            Config = configstore.Load();
            _configStore = configstore;
            _cmdService = cmdService;

            client.GuildAvailable += guild =>
            {
                foreach (var chan in guild.GetTextChannels())
                {
                    if (Config.ChannelModuleWhitelist[chan.Id].Add(nameof(PermissionsModule)))
                    {
                        //Console.WriteLine($"{DateTime.Now}: ");
                    }
                }
                return Task.CompletedTask;
            };
            client.ChannelCreated += chan =>
            {
                var tChan = chan as ITextChannel;
                if (tChan != null && Config.ChannelModuleWhitelist[tChan.Id].Add(nameof(PermissionsModule)))
                {
                    Console.WriteLine($"{DateTime.Now}: Added permission management to {tChan.Name}.");
                }
                return Task.CompletedTask;
            };
            client.ChannelDestroyed += chan =>
            {
                var tChan = chan as ITextChannel;
                if (tChan != null && Config.ChannelModuleWhitelist[chan.Id].Remove(nameof(PermissionsModule)))
                {
                    Console.WriteLine($"{DateTime.Now}: Removed permission management from {tChan.Name}.");
                }
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="cmdname"></param>
        /// <returns></returns>
        [Command("help"), Permission(MinimumPermission.Everyone)]
        [Summary("Display commands you can use or how to use them.")]
        public async Task HelpCmd(IUserMessage msg, string cmdname = null)
        {
            var sb = new StringBuilder();
            if (cmdname == null)
            {
                var cmds = _cmdService.Commands
                    .Where(c => c.CheckPreconditions(msg).GetAwaiter().GetResult().IsSuccess &&
                        !c.Source.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(HiddenAttribute))))
                    .GroupBy(c => c.Name);

                sb.AppendLine("You can use the following commands:")
                    .AppendLine($"`{String.Join("`, `", cmds.SelectMany(g => g.Select(c => c.Name)).Distinct())}`\n")
                    .Append("You can use `help <command>` for more information on that command.");
            }
            else
            {
                var cmds = _commandLookup[cmdname]
                    .Where(c => c.CheckPreconditions(msg).GetAwaiter().GetResult().IsSuccess &&
                        !c.Source.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(HiddenAttribute))));

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
                return $"[({param.ElementType.Name}): {param.Name}...]";
            }
            else if (param.IsRemainder) //&& IsOptional - decided not to check for the combination
            {
                return $"<({param.ElementType.Name}): {param.Name}...>";
            }
            else if (param.IsOptional)
            {
                return $"[({param.ElementType.Name}): {param.Name}]";
            }
            else
            {
                return $"<({param.ElementType.Name}): {param.Name}>";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [Command("roles"), Permission(MinimumPermission.GuildOwner)]
        [Summary("List this server's roles and their ID.")]
        public async Task ListRoles(IUserMessage msg)
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
        public async Task ListModules(IUserMessage msg)
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
        /// <param name="role"></param>
        /// <returns></returns>
        [Command("setadmin"), Permission(MinimumPermission.GuildOwner)]
        [Summary("Set the admin role for this server.")]
        public async Task SetAdminRole(IUserMessage msg, IRole role)
        {
            var ch = msg.Channel as IGuildChannel;
            if (ch != null)
            {
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
        /// <param name="role"></param>
        /// <returns></returns>
        [Command("setmod"), Permission(MinimumPermission.GuildOwner)]
        [Summary("Set the moderator role for this server.")]
        public async Task SetModRole(IUserMessage msg, IRole role)
        {
            var ch = msg.Channel as IGuildChannel;
            if (ch != null)
            {
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
        [Alias("addsp")]
        [Summary("Give someone special command priveliges in this channel.")]
        public async Task AddSpecialUser(IUserMessage msg, IUser user)
        {
            var list = Config.SpecialPermissionUsersList[msg.Channel.Id];
            if (list.Add(user.Id))
            {
                _configStore.Save(Config);
                await msg.Channel.SendMessageAsync($"Gave **{user.Username}** Special command priveliges.");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        [Command("remspecial"), Permission(MinimumPermission.AdminRole)]
        [Alias("remsp")]
        [Summary("Remove someone's special command priveliges in this channel.")]
        public async Task RemoveSpecialUser(IUserMessage msg, IUser user)
        {
            var list = Config.SpecialPermissionUsersList[msg.Channel.Id];
            if (list.Remove(user.Id))
            {
                _configStore.Save(Config);
                await msg.Channel.SendMessageAsync($"Removed **{user.Username}** Special command priveliges.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="modName"></param>
        /// <returns></returns>
        [Command("whitelist"), Permission(MinimumPermission.AdminRole)]
        [Alias("wl")]
        [Summary("Whitelist a module for this channel.")]
        public async Task WhitelistModule(IUserMessage msg, string modName)
        {
            var ch = msg.Channel as IGuildChannel;
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Name == modName);
            if (mod != null)
            {
                if (Config.ChannelModuleWhitelist[ch.Id].Add(mod.Name))
                {
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
        [Command("blacklist"), Permission(MinimumPermission.AdminRole)]
        [Alias("bl")]
        [Summary("Blacklist a module for this channel.")]
        public async Task BlacklistModule(IUserMessage msg, string modName)
        {
            var ch = msg.Channel as IGuildChannel;
            var mod = _cmdService.Modules.SingleOrDefault(m => m.Name == modName);
            if (mod != null)
            {
                if (Config.ChannelModuleWhitelist[ch.Id].Remove(mod.Name))
                {
                    _configStore.Save(Config);
                    await msg.Channel.SendMessageAsync($"Module `{mod.Name}` is now blacklisted in this channel.");
                }
            }
        }
    }
}
