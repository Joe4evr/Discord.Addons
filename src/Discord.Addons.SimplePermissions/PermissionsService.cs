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
        internal readonly Func<LogMessage, Task> Logger;

        /// <summary> </summary>
        /// <param name="configstore"></param>
        /// <param name="commands"></param>
        /// <param name="client"></param>
        internal PermissionsService(
            IConfigStore<IPermissionConfig> configstore,
            CommandService commands,
            DiscordSocketClient client,
            Func<LogMessage, Task> logAction)
        {
            ConfigStore = configstore ?? throw new ArgumentNullException(nameof(configstore));
            CService = commands ?? throw new ArgumentNullException(nameof(commands));
            _client = client ?? throw new ArgumentNullException(nameof(client));

            Logger = logAction;

            client.Connected += checkDuplicateModuleNames;

            client.GuildAvailable += guild => ConfigStore.Load().AddNewGuild(guild);
            client.UserJoined += user => ConfigStore.Load().AddUser(user);
            client.ChannelCreated += chan => ConfigStore.Load().AddChannel(chan);
            client.ChannelDestroyed += chan => ConfigStore.Load().RemoveChannel(chan);

            logAction.Invoke(new LogMessage(LogSeverity.Info, "SimplePermissions", "Created Permission service."));
        }

        private Task checkDuplicateModuleNames()
        {
            var modnames = CService.Modules.Select(m => m.Name).ToList();
            var multiples = modnames.Where(name => modnames.Count(str => str.Equals(name, StringComparison.OrdinalIgnoreCase)) > 1);

            if (multiples.Any())
            {
                throw new Exception(
$@"Multiple modules with the same Name have been registered, SimplePermissions cannot function.
Duplicate names: {String.Join(", ", multiples.Distinct())}.");
            }

            _client.Connected -= checkDuplicateModuleNames;
            return Task.CompletedTask;
        }

        //private void RemovePermissionsModule(IMessageChannel channel)
        //{
        //    ConfigStore.Load().BlacklistModule(channel, PermissionsModule.PermModuleName);
        //    ConfigStore.Save();
        //    Console.WriteLine($"{DateTime.Now}: Removed permission management from {channel.Name}.");
        //}

        //private void AddPermissionsModule(IMessageChannel channel)
        //{
        //    ConfigStore.Load().WhitelistModule(channel, PermissionsModule.PermModuleName);
        //    ConfigStore.Save();
        //    Console.WriteLine($"{DateTime.Now}: Added permission management to {channel.Name}.");
        //}

        internal async Task<bool> SetGuildAdminRole(IGuild guild, IRole role)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            var result = await ConfigStore.Load().SetGuildAdminRole(guild, role).ConfigureAwait(false);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> SetGuildModRole(IGuild guild, IRole role)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            var result = await ConfigStore.Load().SetGuildModRole(guild, role).ConfigureAwait(false);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> AddSpecialUser(IChannel channel, IGuildUser user)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            var result = await ConfigStore.Load().AddSpecialUser(channel, user).ConfigureAwait(false);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> RemoveSpecialUser(IChannel channel, IGuildUser user)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            var result = await ConfigStore.Load().RemoveSpecialUser(channel, user).ConfigureAwait(false);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> WhitelistModule(IChannel channel, string modName)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            var result = await ConfigStore.Load().WhitelistModule(channel, modName).ConfigureAwait(false);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> BlacklistModule(IChannel channel, string modName)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            var result = await ConfigStore.Load().BlacklistModule(channel, modName).ConfigureAwait(false);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> WhitelistModuleGuild(IGuild guild, string modName)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            var result = await ConfigStore.Load().WhitelistModuleGuild(guild, modName).ConfigureAwait(false);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> BlacklistModuleGuild(IGuild guild, string modName)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            var result = await ConfigStore.Load().BlacklistModuleGuild(guild, modName).ConfigureAwait(false);
            ConfigStore.Save();
            _lock.Release();
            return result;
        }
    }

    public static class PermissionsExtensions
    {
        /// <summary> Add SimplePermissions to your <see cref="CommandService"/>. </summary>
        /// <param name="client">The <see cref="DiscordSocketClient"/> instance.</param>
        /// <param name="configStore">The <see cref="IConfigStore{TConfig}"/> instance.</param>
        /// <param name="map">The <see cref="IDependencyMap"/> instance.</param>
        /// <param name="logAction">Optional: A delegate or method that will log messages.</param>
        public static Task AddPermissionsService(
            this CommandService cmdService,
            DiscordSocketClient client,
            IConfigStore<IPermissionConfig> configStore,
            IDependencyMap map,
            Func<LogMessage, Task> logAction = null)
        {
            map.Add(new PermissionsService(configStore, cmdService, client, logAction ?? (msg => Task.CompletedTask)));
            return cmdService.AddModuleAsync<PermissionsModule>();
        }

        internal static bool CanReadAndWrite(this IGuildUser user, ITextChannel channel)
        {
            var clientPerms = user.GetPermissions(channel);
            return (clientPerms.ReadMessages && clientPerms.SendMessages);
        }
    }
}
