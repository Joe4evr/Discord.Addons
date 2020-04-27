using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Core;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public sealed partial class PermissionsService
    {
        private static readonly Emoji _litter = new Emoji("\uD83D\uDEAE");

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        //private readonly CachedConfig _cachedConfig;

        private BaseSocketClient SocketClient { get; }
        private Func<LogMessage, Task> Logger { get; }
        private IConfigStore<IPermissionConfig> ConfigStore { get; }
        private ConcurrentDictionary<ulong, FancyHelpMessage> Helpmsgs { get; }
            = new ConcurrentDictionary<ulong, FancyHelpMessage>();

        internal CommandService CService { get; }
        //internal IPermissionConfig ReadOnlyConfig => _cachedConfig;

        /// <summary> </summary>
        /// <param name="configstore"></param>
        /// <param name="commands"></param>
        /// <param name="client"></param>
        /// <param name="logAction"></param>
        public PermissionsService(
            IConfigStore<IPermissionConfig> configstore,
            CommandService commands,
            BaseSocketClient client,
            Func<LogMessage, Task>? logAction = null)
        {
            Logger = logAction ?? Extensions.NoOpLogger;
            Log(LogSeverity.Info, "Creating Permission service.");

            ConfigStore  = configstore ?? throw new ArgumentNullException(nameof(configstore));
            CService     = commands    ?? throw new ArgumentNullException(nameof(commands));
            SocketClient = client      ?? throw new ArgumentNullException(nameof(client));

            //client.Ready += CheckDuplicateModuleNames;
            client.GuildAvailable   += GuildAvailable;
            client.UserJoined       += UserJoined;
            client.ChannelCreated   += ChannelCreated;
            client.ChannelDestroyed += ChannelDestroyed;
            client.ReactionAdded    += ReactionAdded;
            client.MessageDeleted   += MessageDeleted;
        }

        //public PermissionsService(
        //    IConfigStore<IPermissionConfig> configstore,
        //    CommandService commands,
        //    DiscordShardedClient client,
        //    Func<LogMessage, Task> logAction = null) : this(configstore, commands, logAction)
        //{
        //    ShardedClient = client ?? throw new ArgumentNullException(nameof(client));

        //    client.GetShard(0).Ready += CheckDuplicateModuleNames;
        //    client.GuildAvailable += GuildAvailable;
        //    client.UserJoined += UserJoined;
        //    client.ChannelCreated += ChannelCreated;
        //    client.ChannelDestroyed += ChannelDestroyed;
        //    client.ReactionAdded += ReactionAdded;
        //    client.MessageDeleted += MessageDeleted;
        //}

        #region PermissionEvents
        private Task GuildAvailable(SocketGuild guild)
        {
            Task.Run(async () =>
            {
                using var config = ConfigStore.Load();
                await guild.DownloadUsersAsync().ConfigureAwait(false);
                await config.AddNewGuild(guild, guild.Users).ConfigureAwait(false);
                config.Save();
            });
            return Task.CompletedTask;
        }

        private Task UserJoined(SocketGuildUser user)
        {
            Task.Run(async () =>
            {
                using var config = ConfigStore.Load();
                await config.AddUser(user).ConfigureAwait(false);
                config.Save();
            });
            return Task.CompletedTask;
        }

        private Task ChannelCreated(SocketChannel channel)
        {
            Task.Run(async () =>
            {
                if (channel is SocketTextChannel textChannel)
                {
                    using var config = ConfigStore.Load();
                    await config.AddChannel(textChannel).ConfigureAwait(false);
                    config.Save();
                }
            });
            return Task.CompletedTask;
        }

        private Task ChannelDestroyed(SocketChannel channel)
        {
            Task.Run(async () =>
            {
                if (channel is SocketTextChannel textChannel)
                {
                    using var config = ConfigStore.Load();
                    await config.RemoveChannel(textChannel).ConfigureAwait(false);
                    config.Save();
                }
            });
            return Task.CompletedTask;
        }
        #endregion

        private Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Task.Run(async () =>
            {
                if (!message.HasValue)
                {
                    await Log(LogSeverity.Debug, $"Message with id {message.Id} was not in cache.");
                    return;
                }
                if (!reaction.User.IsSpecified)
                {
                    await Log(LogSeverity.Debug, $"Reaction on message with id {message.Id} had invalid user.");
                    return;
                }

                var msg = message.Value;
                var iclient = (SocketClient as IDiscordClient); //?? (ShardedClient as IDiscordClient);
                if (reaction.UserId == iclient.CurrentUser.Id)
                {
                    return;
                }
                if (msg.Author.Id == iclient.CurrentUser.Id
                    && reaction.UserId == (await iclient.GetApplicationInfoAsync()).Owner.Id
                    && reaction.Emote.Name == _litter.Name)
                {

                    await msg.DeleteAsync();
                    return;
                }

                if (Helpmsgs.TryGetValue(msg.Id, out var fhm))
                {
                    if (reaction.UserId != fhm.UserId)
                    {
                        var _ = msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        return;
                    }

                    switch (reaction.Emote.Name)
                    {
                        case FancyHelpMessage.SFirst:
                            await fhm.First();
                            break;
                        case FancyHelpMessage.SBack:
                            await fhm.Back();
                            break;
                        case FancyHelpMessage.SNext:
                            await fhm.Next();
                            break;
                        case FancyHelpMessage.SLast:
                            await fhm.Last();
                            break;
                        case FancyHelpMessage.SDelete:
                            await fhm.Delete();
                            break;
                        default:
                            break;
                    }
                }
            });
            return Task.CompletedTask;
        }

        private Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            return Task.FromResult(Helpmsgs.TryRemove(message.Id, out _));
        }

    //    private async Task CheckDuplicateModuleNames()
    //    {
    //        var modnames = CService.Modules.Select(m => m.Name).ToList();
    //        var multiples = modnames.Where(name => modnames.Count(str => str.Equals(name, StringComparison.OrdinalIgnoreCase)) > 1);

    //        if (multiples.Any())
    //        {
    //            var error = $@"Multiple modules with the same Name have been registered, SimplePermissions cannot function.
    //Duplicate names: {String.Join(", ", multiples.Distinct())}.";
    //            await Log(LogSeverity.Error, error).ConfigureAwait(false);
    //            throw new Exception(error);
    //        }

    //        if (SocketClient != null)
    //        {
    //            SocketClient.Ready -= CheckDuplicateModuleNames;
    //            //using (var config = ConfigStore.Load())
    //            //{
    //            //    await _cachedConfig.Synchronize(SocketClient, config).ConfigureAwait(false);
    //            //}
    //        }
    //        //if (ShardedClient != null)
    //        //    ShardedClient.GetShard(0).Ready -= CheckDuplicateModuleNames;



    //    }

        internal IPermissionConfig LoadConfig() => ConfigStore.Load();

        internal Task Log(LogSeverity severity, string msg)
        {
            return Logger(new LogMessage(severity, "SimplePermissions", msg));
        }

        internal Task AddNewFancy(FancyHelpMessage fhm)
        {
            if (fhm.MsgId > 0UL)
                Helpmsgs.TryAdd(fhm.MsgId, fhm);

            return Task.CompletedTask;
        }

        internal async Task<bool> SetGuildAdminRole(IRole role)
        {
            using var config = ConfigStore.Load();
            await _lock.WaitAsync();
            var result = await config.SetGuildAdminRole(role).ConfigureAwait(false);
            config.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> SetGuildModRole(IRole role)
        {
            using var config = ConfigStore.Load();
            await _lock.WaitAsync();
            var result = await config.SetGuildModRole(role).ConfigureAwait(false);
            config.Save();
            _lock.Release();
            return result;
        }

        internal async Task<bool> AddSpecialUser(ITextChannel channel, IGuildUser user)
        {
            using var config = ConfigStore.Load();
            await _lock.WaitAsync();
            var result = await config.AddSpecialUser(channel, user).ConfigureAwait(false);
            config.Save();
            //await ReadOnlyConfig.AddSpecialUser(channel, user).ConfigureAwait(false);
            _lock.Release();
            return result;
        }

        internal async Task<bool> RemoveSpecialUser(ITextChannel channel, IGuildUser user)
        {
            using var config = ConfigStore.Load();
            await _lock.WaitAsync();
            var result = await config.RemoveSpecialUser(channel, user).ConfigureAwait(false);
            config.Save();
            //await ReadOnlyConfig.RemoveSpecialUser(channel, user).ConfigureAwait(false);
            _lock.Release();
            return result;
        }

        internal async Task<bool> WhitelistModule(ITextChannel channel, ModuleInfo module)
        {
            using var config = ConfigStore.Load();
            await _lock.WaitAsync();
            var result = await config.WhitelistModule(channel, module).ConfigureAwait(false);
            config.Save();
            //await ReadOnlyConfig.WhitelistModule(channel, module).ConfigureAwait(false);
            _lock.Release();
            return result;
        }

        internal async Task<bool> BlacklistModule(ITextChannel channel, ModuleInfo module)
        {
            using var config = ConfigStore.Load();
            await _lock.WaitAsync();
            var result = await config.BlacklistModule(channel, module).ConfigureAwait(false);
            config.Save();
            //await ReadOnlyConfig.BlacklistModule(channel, module).ConfigureAwait(false);
            _lock.Release();
            return result;
        }

        internal async Task<bool> WhitelistModuleGuild(IGuild guild, ModuleInfo module)
        {
            using var config = ConfigStore.Load();
            await _lock.WaitAsync();
            var result = await config.WhitelistModuleGuild(guild, module).ConfigureAwait(false);
            config.Save();
            //await ReadOnlyConfig.WhitelistModuleGuild(guild, module).ConfigureAwait(false);
            _lock.Release();
            return result;
        }

        internal async Task<bool> BlacklistModuleGuild(IGuild guild, ModuleInfo module)
        {
            using var config = ConfigStore.Load();
            await _lock.WaitAsync();
            var result = await config.BlacklistModuleGuild(guild, module).ConfigureAwait(false);
            config.Save();
            //await ReadOnlyConfig.BlacklistModuleGuild(guild, module).ConfigureAwait(false);
            _lock.Release();
            return result;
        }

        internal async Task SetHidePermCommands(IGuild guild, bool newValue)
        {
            using var config = ConfigStore.Load();
            await _lock.WaitAsync();
            await config.SetHidePermCommands(guild, newValue).ConfigureAwait(false);
            config.Save();
            //await ReadOnlyConfig.SetHidePermCommands(guild, newValue).ConfigureAwait(false);
            _lock.Release();
        }

        internal static int GetMessageCacheSize(DiscordSocketClient client)
        {
            var p = typeof(DiscordSocketClient).GetProperty("MessageCacheSize", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (int)p.GetMethod!.Invoke(client, Array.Empty<object>())!;
        }
    }
}
