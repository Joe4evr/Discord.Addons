using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PermissionsService
    {
        private readonly CommandService _cService;
        private readonly DiscordSocketClient _client;

        internal readonly IConfigStore<IPermissionConfig> ConfigStore;
        internal readonly IPermissionConfig Config;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configstore"></param>
        /// <param name="commands"></param>
        /// <param name="client"></param>
        public PermissionsService(
            IConfigStore<IPermissionConfig> configstore,
            CommandService commands,
            DiscordSocketClient client)
        {
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (configstore == null) throw new ArgumentNullException(nameof(configstore));
            if (client == null) throw new ArgumentNullException(nameof(client));

            _cService = commands;
            _client = client;
            Config = configstore.Load();
            ConfigStore = configstore;

            client.Connected += checkDuplicateModuleNames;

            client.GuildAvailable += async guild =>
            {
                await Config.AddNewGuild(guild);
                ConfigStore.Save();

                foreach (var chan in await guild.GetTextChannelsAsync())
                {
                    if (await CanReadAndWrite(chan))
                    {
                        if (!Config.GetChannelModuleWhitelist(chan.Id).Contains(PermissionsModule.permModuleName))
                        {
                            AddPermissionsModule(chan);
                        }
                    }
                }
            };
            client.ChannelCreated += async chan =>
            {
                var mChan = chan as IMessageChannel;
                if (mChan != null && (await CanReadAndWrite(mChan)))
                {
                    AddPermissionsModule(mChan);
                }
            };
            client.ChannelDestroyed += chan =>
            {
                var mChan = chan as IMessageChannel;
                if (mChan != null)
                {
                    Config.RemoveChannel(mChan.Id);
                }
                return Task.CompletedTask;
            };
            client.ChannelUpdated += async (before, after) =>
            {
                var mChan = after as IMessageChannel;
                if (mChan != null && (await CanReadAndWrite(mChan)))
                {
                    AddPermissionsModule(mChan);
                }
                else if (Config.GetChannelModuleWhitelist(after.Id).Contains(PermissionsModule.permModuleName))
                {
                    RemovePermissionsModule(mChan);
                }
            };
        }

        private Task checkDuplicateModuleNames()
        {
            var modnames = _cService.Modules.Select(m => m.Name).ToList();
            var multiples = modnames.Where(name => modnames.Count(str => str.Equals(name, StringComparison.OrdinalIgnoreCase)) > 1);

            if (multiples.Count() > 0)
            {
                throw new Exception(
$@"Multiple modules with the same Name have been registered, SimplePermissions cannot function.
Duplicate names: {String.Join(", ", multiples.Distinct())}.");
            }

            _client.Connected -= checkDuplicateModuleNames;
            return Task.CompletedTask;
        }

        private void RemovePermissionsModule(IMessageChannel channel)
        {
            Config.BlacklistModule(channel.Id, PermissionsModule.permModuleName);
            ConfigStore.Save();
            Console.WriteLine($"{DateTime.Now}: Removed permission management from {channel.Name}.");
        }

        private void AddPermissionsModule(IMessageChannel channel)
        {
            Config.WhitelistModule(channel.Id, PermissionsModule.permModuleName);
            ConfigStore.Save();
            Console.WriteLine($"{DateTime.Now}: Added permission management to {channel.Name}.");
        }

        private async Task<bool> CanReadAndWrite(IMessageChannel channel)
        {
            var tChan = channel as ITextChannel;
            if (tChan != null)
            {
                var clientPerms = (await tChan.Guild.GetCurrentUserAsync()).GetPermissions(tChan);
                return (clientPerms.ReadMessages && clientPerms.SendMessages);
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveConfig() => ConfigStore.Save();
    }
}
