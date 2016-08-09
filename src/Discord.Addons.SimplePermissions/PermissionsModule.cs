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
                    .Where(c => c.GetType().GetTypeInfo().GetCustomAttribute<PreconditionAttribute>()
                        .CheckPermissions(msg, c, this).GetAwaiter().GetResult().IsSuccess)
                    .Select(c => c.Name);
                sb.AppendLine("You can use the following commands:")
                    .AppendLine($"`{String.Join("`, `", cmds)}`\n");

            }
            else
            {
                var cmds = _commandLookup[cmdname]
                    .Where(c => c.GetType().GetTypeInfo().GetCustomAttribute<PreconditionAttribute>()
                        .CheckPermissions(msg, c, this).GetAwaiter().GetResult().IsSuccess);
                if (cmds.Count() > 1)
                {
                    sb.AppendLine(cmds.First().Name);
                }
                else return;
            }
            await msg.Channel.SendMessageAsync(sb.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [Command("roles"), Permission(MinimumPermission.GuildOwner)]
        [Description("List this server's roles and their ID.")]
        public async Task ListRoles(IMessage msg)
        {
            var ch = msg.Channel as IGuildChannel;
            if (ch != null)
            {
                await msg.Channel.SendMessageAsync(
                    $"This server's roles\n {String.Join("\n", ch.Guild.Roles.Where(r => r.Id != ch.Guild.EveryoneRole.Id).Select(r => $"{r.Name} : {r.Id}"))}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [Command("setadmin"), Permission(MinimumPermission.GuildOwner)]
        [Description("Set the admin role for this server.")]
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
        [Description("Set the admin role for this server.")]
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

                Config.GuildAdminRole[ch.Guild.Id] = role.Id;
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
        [Description("Give someone special command priveliges in this channel.")]
        public async Task AddSpecialUser(IMessage msg, IUser user)
        {
            var list = Config.SpecialPermissionUsersList[msg.Channel.Id];
            if (!list.Contains(user.Id))
            {
                list.Add(user.Id);
                await msg.Channel.SendMessageAsync($"Gave **{user.Username}** Special command priveliges.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="cmdName"></param>
        /// <returns></returns>
        [Command("wl"), Permission(MinimumPermission.AdminRole)]
        [Description("Whitelist a command for this channel.")]
        public async Task WhitelistCommand(IMessage msg, string cmdName)
        {
            var ch = msg.Channel as IGuildChannel;
            var cmds = _commandLookup[cmdName];
            if (cmds.Count() > 1)
            {
                if (!Config.ChannelCommandWhitelist[ch.Id].Contains(cmdName))
                {
                    Config.ChannelCommandWhitelist[ch.Id].Add(cmdName);
                    _configStore.Save(Config);
                    await msg.Channel.SendMessageAsync($"Command `{cmds.First().Name}` is now whitelisted in this channel.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="cmdName"></param>
        /// <returns></returns>
        [Command("bl"), Permission(MinimumPermission.AdminRole)]
        [Description("Blacklist a command for this channel.")]
        public async Task BlacklistCommand(IMessage msg, string cmdName)
        {
            var ch = msg.Channel as IGuildChannel;
            var cmds = _commandLookup[cmdName];
            if (cmds.Count() > 1)
            {
                if (Config.ChannelCommandWhitelist[ch.Id].Contains(cmdName))
                {
                    Config.ChannelCommandWhitelist[ch.Id].Remove(cmdName);
                    _configStore.Save(Config);
                    await msg.Channel.SendMessageAsync($"Command `{cmds.First().Name}` is now blacklisted in this channel.");
                }
            }
        }
    }
}
