using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.SimpleConfig;

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
                if (!Config.GuildAdminRole.ContainsKey(guild.Id))
                {
                    Config.GuildAdminRole[guild.Id] = 0;
                    ConfigStore.Save();
                }
                if (!Config.GuildModRole.ContainsKey(guild.Id))
                {
                    Config.GuildModRole[guild.Id] = 0;
                    ConfigStore.Save();
                }

                foreach (var chan in await guild.GetTextChannelsAsync())
                {
                    if (await CanReadAndWrite(chan))
                    {
                        if (!Config.ChannelModuleWhitelist.ContainsKey(chan.Id))
                        {
                            Config.ChannelModuleWhitelist[chan.Id] = new HashSet<string>();
                            ConfigStore.Save();
                        }
                        if (!Config.SpecialPermissionUsersList.ContainsKey(chan.Id))
                        {
                            Config.SpecialPermissionUsersList[chan.Id] = new HashSet<ulong>();
                            ConfigStore.Save();
                        }
                        if (Config.ChannelModuleWhitelist[chan.Id].Add(PermissionsModule.permModuleName))
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
                    if (!Config.ChannelModuleWhitelist.ContainsKey(chan.Id))
                    {
                        Config.ChannelModuleWhitelist[chan.Id] = new HashSet<string>();
                    }

                    AddPermissionsModule(mChan);
                }
            };
            client.ChannelDestroyed += chan =>
            {
                var mChan = chan as IMessageChannel;
                if (mChan != null)
                {
                    RemoveChannel(mChan);
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
                else if (Config.ChannelModuleWhitelist.ContainsKey(after.Id))
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

        private void RemoveChannel(IMessageChannel channel)
        {
            if (Config.ChannelModuleWhitelist.ContainsKey(channel.Id))
            {
                Config.ChannelModuleWhitelist.Remove(channel.Id);
            }
        }

        private void RemovePermissionsModule(IMessageChannel channel)
        {
            if (Config.ChannelModuleWhitelist[channel.Id].Remove(PermissionsModule.permModuleName))
            {
                ConfigStore.Save();
                Console.WriteLine($"{DateTime.Now}: Removed permission management from {channel.Name}.");
            }
        }

        private void AddPermissionsModule(IMessageChannel channel)
        {
            if (Config.ChannelModuleWhitelist[channel.Id].Add(PermissionsModule.permModuleName))
            {
                ConfigStore.Save();
                Console.WriteLine($"{DateTime.Now}: Added permission management to {channel.Name}.");
            }
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
