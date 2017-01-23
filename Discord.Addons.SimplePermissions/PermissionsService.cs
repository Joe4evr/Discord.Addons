using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public sealed class PermissionsService
    {
        private readonly DiscordSocketClient _client;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        //private readonly Dictionary<ulong, FancyHelpMessage> _helpmsgs = new Dictionary<ulong, FancyHelpMessage>();

        internal readonly CommandService CService;
        internal readonly IConfigStore<IPermissionConfig> ConfigStore;

        /// <summary> </summary>
        /// <param name="configstore"></param>
        /// <param name="commands"></param>
        /// <param name="client"></param>
        internal PermissionsService(
            IConfigStore<IPermissionConfig> configstore,
            CommandService commands,
            DiscordSocketClient client)
        {
            ConfigStore = configstore ?? throw new ArgumentNullException(nameof(configstore));
            CService = commands ?? throw new ArgumentNullException(nameof(commands));
            _client = client ?? throw new ArgumentNullException(nameof(client));

            client.Connected += checkDuplicateModuleNames;

            client.GuildAvailable += async guild =>
            {
                var config = ConfigStore.Load();
                await config.AddNewGuild(guild);
                ConfigStore.Save();

                foreach (var chan in await guild.GetTextChannelsAsync())
                {
                    if (await CanReadAndWrite(chan))
                    {
                        if (!config.GetChannelModuleWhitelist(chan).Contains(PermissionsModule.permModuleName))
                        {
                            AddPermissionsModule(chan);
                        }
                    }
                }
            };
            client.ChannelCreated += async chan =>
            {
                if (chan is IMessageChannel mChan && (await CanReadAndWrite(mChan)))
                {
                    AddPermissionsModule(mChan);
                }
            };
            client.ChannelDestroyed += chan =>
            {
                if (chan is IMessageChannel mChan)
                {
                    ConfigStore.Load().RemoveChannel(mChan);
                }
                return Task.CompletedTask;
            };
            client.ChannelUpdated += async (before, after) =>
            {
                if (after is IMessageChannel mChan)
                {
                    if (await CanReadAndWrite(mChan))
                    {
                        AddPermissionsModule(mChan);
                    }
                    else if (ConfigStore.Load().GetChannelModuleWhitelist(after).Contains(PermissionsModule.permModuleName))
                    {
                        RemovePermissionsModule(mChan);
                    }
                }
            };
            //client.ReactionAdded += (id, msg, reaction) =>
            //{
            //    return Task.CompletedTask;
            //};
            client.UserJoined += user => ConfigStore.Load().AddUser(user);

            Console.WriteLine($"{DateTime.Now}: Created Permission service.");

        }

        private Task checkDuplicateModuleNames()
        {
            var modnames = CService.Modules.Select(m => m.Name).ToList();
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
            ConfigStore.Load().BlacklistModule(channel, PermissionsModule.permModuleName);
            ConfigStore.Save();
            Console.WriteLine($"{DateTime.Now}: Removed permission management from {channel.Name}.");
        }

        private void AddPermissionsModule(IMessageChannel channel)
        {
            ConfigStore.Load().WhitelistModule(channel, PermissionsModule.permModuleName);
            ConfigStore.Save();
            Console.WriteLine($"{DateTime.Now}: Added permission management to {channel.Name}.");
        }

        private async Task<bool> CanReadAndWrite(IMessageChannel channel)
        {
            if (channel is ITextChannel tChan)
            {
                var clientPerms = (await tChan.Guild.GetCurrentUserAsync()).GetPermissions(tChan);
                return (clientPerms.ReadMessages && clientPerms.SendMessages);
            }

            return true;
        }

        internal async Task<bool> SetGuildAdminRole(IGuild guild, IRole role)
        {
            await _lock.WaitAsync();
            var result = await ConfigStore.Load().SetGuildAdminRole(guild, role);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> SetGuildModRole(IGuild guild, IRole role)
        {
            await _lock.WaitAsync();
            var result = await ConfigStore.Load().SetGuildModRole(guild, role);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> AddSpecialUser(IChannel channel, IGuildUser user)
        {
            await _lock.WaitAsync();
            var result = await ConfigStore.Load().AddSpecialUser(channel, user);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> RemoveSpecialUser(IChannel channel, IGuildUser user)
        {
            await _lock.WaitAsync();
            var result = await ConfigStore.Load().RemoveSpecialUser(channel, user);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> WhitelistModule(IChannel channel, string modName)
        {
            await _lock.WaitAsync();
            var result = await ConfigStore.Load().WhitelistModule(channel, modName);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> BlacklistModule(IChannel channel, string modName)
        {
            await _lock.WaitAsync();
            var result = await ConfigStore.Load().BlacklistModule(channel, modName);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }
    }

    public static class PermissionsExtensions
    {
        public static Task AddPermissionsService(
            this CommandService cmdService,
            DiscordSocketClient client,
            IConfigStore<IPermissionConfig> configStore,
            IDependencyMap map)
        {
            map.Add(new PermissionsService(configStore, cmdService, client));
            return cmdService.AddModuleAsync<PermissionsModule>();
        }
    }
}
