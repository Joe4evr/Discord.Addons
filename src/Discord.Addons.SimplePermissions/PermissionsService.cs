using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public sealed class PermissionsService
    {
        private static readonly Emoji _litter = new Emoji("\uD83D\uDEAE");

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private DiscordSocketClient SocketClient { get; }
        private DiscordShardedClient ShardedClient { get; }
        private Func<LogMessage, Task> Logger { get; }
        private ConcurrentDictionary<ulong, FancyHelpMessage> Helpmsgs { get; } = new ConcurrentDictionary<ulong, FancyHelpMessage>();

        internal CommandService CService { get; }
        internal IConfigStore<IPermissionConfig> ConfigStore { get; }

        private PermissionsService(
            IConfigStore<IPermissionConfig> configstore,
            CommandService commands,
            Func<LogMessage, Task> logAction)
        {
            Logger = logAction;
            Log(LogSeverity.Info, "Creating Permission service.");

            ConfigStore = configstore ?? throw new ArgumentNullException(nameof(configstore));
            CService = commands ?? throw new ArgumentNullException(nameof(commands));
        }

        /// <summary> </summary>
        /// <param name="configstore"></param>
        /// <param name="commands"></param>
        /// <param name="client"></param>
        internal PermissionsService(
            IConfigStore<IPermissionConfig> configstore,
            CommandService commands,
            DiscordSocketClient client,
            Func<LogMessage, Task> logAction) : this(configstore, commands, logAction)
        {
            SocketClient = client ?? throw new ArgumentNullException(nameof(client));

            client.Ready += CheckDuplicateModuleNames;
            client.GuildAvailable += GuildAvailable;
            client.UserJoined += UserJoined;
            client.ChannelCreated += ChannelCreated;
            client.ChannelDestroyed += ChannelDestroyed;
            client.ReactionAdded += ReactionAdded;
            client.MessageDeleted += MessageDeleted;
        }

        internal PermissionsService(
            IConfigStore<IPermissionConfig> configstore,
            CommandService commands,
            DiscordShardedClient client,
            Func<LogMessage, Task> logAction) : this(configstore, commands, logAction)
        {
            ShardedClient = client ?? throw new ArgumentNullException(nameof(client));

            client.GetShard(0).Ready += CheckDuplicateModuleNames;
            client.GuildAvailable += GuildAvailable;
            client.UserJoined += UserJoined;
            client.ChannelCreated += ChannelCreated;
            client.ChannelDestroyed += ChannelDestroyed;
            client.ReactionAdded += ReactionAdded;
            client.MessageDeleted += MessageDeleted;
        }

#region PermissionEvents
        private async Task GuildAvailable(SocketGuild guild)
        {
            using (var config = ConfigStore.Load())
            {
                await config.AddNewGuild(guild);
                config.Save();
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            using (var config = ConfigStore.Load())
            {
                await config.AddUser(user);
                config.Save();
            }
        }

        private async Task ChannelCreated(SocketChannel channel)
        {
            if (channel is SocketTextChannel textChannel)
            {
                using (var config = ConfigStore.Load())
                {
                    await config.AddChannel(textChannel);
                    config.Save();
                }
            }
        }

        private async Task ChannelDestroyed(SocketChannel channel)
        {
            if (channel is SocketTextChannel textChannel)
            {
                using (var config = ConfigStore.Load())
                {
                    await config.RemoveChannel(textChannel);
                    config.Save();
                }
            }
        }
#endregion

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!message.HasValue)
            {
                await Log(LogSeverity.Debug, $"Message with id {message.Id} was not in cache.");
                return;
            }
            if (!reaction.User.IsSpecified)
            {
                await Log(LogSeverity.Debug, $"Message with id {message.Id} had invalid user.");
                return;
            }

            var msg = message.Value;
            var iclient = (SocketClient as IDiscordClient) ?? (ShardedClient as IDiscordClient);
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
        }

        private Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            return Task.FromResult(Helpmsgs.TryRemove(message.Id, out var _));
        }

        private async Task CheckDuplicateModuleNames()
        {
            var modnames = CService.Modules.Select(m => m.Name).ToList();
            var multiples = modnames.Where(name => modnames.Count(str => str.Equals(name, StringComparison.OrdinalIgnoreCase)) > 1);

            if (multiples.Any())
            {
                var error = $@"Multiple modules with the same Name have been registered, SimplePermissions cannot function.
    Duplicate names: {String.Join(", ", multiples.Distinct())}.";
                await Log(LogSeverity.Error, error);
                throw new Exception(error);
            }

            if (SocketClient != null)
                SocketClient.Ready -= CheckDuplicateModuleNames;

            if (ShardedClient != null)
                ShardedClient.GetShard(0).Ready -= CheckDuplicateModuleNames;
        }

        internal Task Log(LogSeverity severity, string msg)
        {
            return Logger(new LogMessage(severity, "SimplePermissions", msg));
        }

        internal Task AddNewFancy(FancyHelpMessage fhm)
        {
            Helpmsgs.TryAdd(fhm.MsgId, fhm);
            return Task.CompletedTask;
        }

        internal async Task<bool> SetGuildAdminRole(IGuild guild, IRole role)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                var result = await config.SetGuildAdminRole(guild, role);
                config.Save();
                _lock.Release();
                return result;
            }
        }

        internal async Task<bool> SetGuildModRole(IGuild guild, IRole role)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                var result = await config.SetGuildModRole(guild, role);
                config.Save();
                _lock.Release();
                return result;
            }
        }

        internal async Task<bool> AddSpecialUser(ITextChannel channel, IGuildUser user)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                var result = await config.AddSpecialUser(channel, user);
                config.Save();
                _lock.Release();
                return result;
            }
        }

        internal async Task<bool> RemoveSpecialUser(ITextChannel channel, IGuildUser user)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                var result = await config.RemoveSpecialUser(channel, user);
                config.Save();
                _lock.Release();
                return result;
            }
        }

        internal async Task<bool> WhitelistModule(ITextChannel channel, string modName)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                var result = await config.WhitelistModule(channel, modName);
                config.Save();
                _lock.Release();
                return result;
            }
        }

        internal async Task<bool> BlacklistModule(ITextChannel channel, string modName)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                var result = await config.BlacklistModule(channel, modName);
                config.Save();
                _lock.Release();
                return result;
            }
        }

        internal async Task<bool> WhitelistModuleGuild(IGuild guild, string modName)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                var result = await config.WhitelistModuleGuild(guild, modName);
                config.Save();
                _lock.Release();
                return result;
            }
        }

        internal async Task<bool> BlacklistModuleGuild(IGuild guild, string modName)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                var result = await config.BlacklistModuleGuild(guild, modName);
                config.Save();
                _lock.Release();
                return result;
            }
        }

        internal async Task<bool> GetHidePermCommands(IGuild guild)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                var result = await config.GetHidePermCommands(guild);
                config.Save();
                _lock.Release();
                return result;
            }
        }

        internal async Task HidePermCommands(IGuild guild, bool newValue)
        {
            using (var config = ConfigStore.Load())
            {
                await _lock.WaitAsync();
                await config.SetHidePermCommands(guild, newValue);
                config.Save();
                _lock.Release();
            }
        }

        internal static int GetMessageCacheSize(DiscordSocketClient client)
        {
            var p = typeof(DiscordSocketClient).GetProperty("MessageCacheSize", BindingFlags.Instance | BindingFlags.NonPublic);
            return (int)p.GetMethod.Invoke(client, Array.Empty<object>());
        }
    }

    public static class PermissionsExtensions
    {
        /// <summary> Add SimplePermissions to your <see cref="CommandService"/> using a <see cref="DiscordSocketClient"/>. </summary>
        /// <param name="client">The <see cref="DiscordSocketClient"/> instance.</param>
        /// <param name="configStore">The <see cref="IConfigStore{TConfig}"/> instance.</param>
        /// <param name="map">The <see cref="IDependencyMap"/> instance.</param>
        /// <param name="logAction">Optional: A delegate or method that will log messages.</param>
        public static Task UseSimplePermissions(
            this CommandService cmdService,
            DiscordSocketClient client,
            IConfigStore<IPermissionConfig> configStore,
            IServiceCollection map,
            Func<LogMessage, Task> logAction = null)
        {
            map.AddSingleton(new PermissionsService(configStore, cmdService, client, logAction ?? (msg => Task.CompletedTask)));
            return cmdService.AddModuleAsync<PermissionsModule>();
        }

        /// <summary> Add SimplePermissions to your <see cref="CommandService"/> using a <see cref="DiscordShardedClient"/>. </summary>
        /// <param name="client">The <see cref="DiscordShardedClient"/> instance.</param>
        /// <param name="configStore">The <see cref="IConfigStore{TConfig}"/> instance.</param>
        /// <param name="map">The <see cref="IDependencyMap"/> instance.</param>
        /// <param name="logAction">Optional: A delegate or method that will log messages.</param>
        public static Task UseSimplePermissions(
            this CommandService cmdService,
            DiscordShardedClient client,
            IConfigStore<IPermissionConfig> configStore,
            IServiceCollection map,
            Func<LogMessage, Task> logAction = null)
        {
            map.AddSingleton(new PermissionsService(configStore, cmdService, client, logAction ?? (msg => Task.CompletedTask)));
            return cmdService.AddModuleAsync<PermissionsModule>();
        }

        internal static bool HasPerms(this IGuildUser user, IGuildChannel channel, DiscordPermissions perms)
        {
            var clientPerms = (DiscordPermissions)user.GetPermissions(channel).RawValue;
            return (clientPerms & perms) == perms;
        }

        [Flags]
        internal enum DiscordPermissions : ulong
        {
            CREATE_INSTANT_INVITE = 0x00_00_00_01,
            KICK_MEMBERS          = 0x00_00_00_02,
            BAN_MEMBERS           = 0x00_00_00_04,
            ADMINISTRATOR         = 0x00_00_00_08,
            MANAGE_CHANNELS       = 0x00_00_00_10,
            MANAGE_GUILD          = 0x00_00_00_20,
            ADD_REACTIONS         = 0x00_00_00_40,
            READ_MESSAGES         = 0x00_00_04_00,
            SEND_MESSAGES         = 0x00_00_08_00,
            SEND_TTS_MESSAGES     = 0x00_00_10_00,
            MANAGE_MESSAGES       = 0x00_00_20_00,
            EMBED_LINKS           = 0x00_00_40_00,
            ATTACH_FILES          = 0x00_00_80_00,
            READ_MESSAGE_HISTORY  = 0x00_01_00_00,
            MENTION_EVERYONE      = 0x00_02_00_00,
            USE_EXTERNAL_EMOJIS   = 0x00_04_00_00,
            CONNECT               = 0x00_10_00_00,
            SPEAK                 = 0x00_20_00_00,
            MUTE_MEMBERS          = 0x00_40_00_00,
            DEAFEN_MEMBERS        = 0x00_80_00_00,
            MOVE_MEMBERS          = 0x01_00_00_00,
            USE_VAD               = 0x02_00_00_00,
            CHANGE_NICKNAME       = 0x04_00_00_00,
            MANAGE_NICKNAMES      = 0x08_00_00_00,
            MANAGE_ROLES          = 0x10_00_00_00,
            MANAGE_WEBHOOKS       = 0x20_00_00_00,
            MANAGE_EMOJIS         = 0x40_00_00_00,
        }
    }
}
