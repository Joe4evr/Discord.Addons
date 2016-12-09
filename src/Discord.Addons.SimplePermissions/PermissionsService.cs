using System;
using System.Linq;
using System.Threading;
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
        private readonly IConfigStore<IPermissionConfig> _configStore;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

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
            //Config = configstore.Load();
            _configStore = configstore;

            client.Connected += checkDuplicateModuleNames;

            client.GuildAvailable += async guild =>
            {
                var config = _configStore.Load();
                await config.AddNewGuild(guild);
                _configStore.Save();

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
                    _configStore.Load().RemoveChannel(mChan);
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
                else if (_configStore.Load().GetChannelModuleWhitelist(after).Contains(PermissionsModule.permModuleName))
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
            _configStore.Load().BlacklistModule(channel, PermissionsModule.permModuleName);
            _configStore.Save();
            Console.WriteLine($"{DateTime.Now}: Removed permission management from {channel.Name}.");
        }

        private void AddPermissionsModule(IMessageChannel channel)
        {
            _configStore.Load().WhitelistModule(channel, PermissionsModule.permModuleName);
            _configStore.Save();
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

        internal async Task<bool> SetGuildAdminRole(IGuild guild, IRole role)
        {
            await _lock.WaitAsync();
            var result = await _configStore.Load().SetGuildAdminRole(guild, role);
            _configStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> SetGuildModRole(IGuild guild, IRole role)
        {
            await _lock.WaitAsync();
            var result = await _configStore.Load().SetGuildModRole(guild, role);
            _configStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> AddSpecialUser(IChannel channel, IUser user)
        {
            await _lock.WaitAsync();
            var result = await _configStore.Load().AddSpecialUser(channel, user);
            _configStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> RemoveSpecialUser(IChannel channel, IUser user)
        {
            await _lock.WaitAsync();
            var result = await _configStore.Load().RemoveSpecialUser(channel, user);
            _configStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> WhitelistModule(IChannel channel, string modName)
        {
            await _lock.WaitAsync();
            var result = await _configStore.Load().WhitelistModule(channel, modName);
            _configStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> BlacklistModule(IChannel channel, string modName)
        {
            await _lock.WaitAsync();
            var result = await _configStore.Load().BlacklistModule(channel, modName);
            _configStore.Save();
            _lock.Release();
            return result;
        }
    }
}
