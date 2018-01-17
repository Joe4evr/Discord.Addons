using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Core;
using System.Collections.Immutable;

namespace Discord.Addons.SimpleAudio
{
    public sealed class AudioService
    {
        private static readonly Embed _initEmbed = new EmbedBuilder { Title = "Connected", Description = "Initializing..." }.Build();
        private Func<LogMessage, Task> Logger { get; }
        //private readonly Timer _presenceChecker;
        private DiscordSocketClient Client { get; }

        internal AudioConfig Config { get; }
        internal ConcurrentDictionary<ulong, AudioClientWrapper> Clients { get; }
            = new ConcurrentDictionary<ulong, AudioClientWrapper>();

        internal ConcurrentDictionary<ulong, EmbedList> Lists { get; }
            = new ConcurrentDictionary<ulong, EmbedList>();

        public AudioService(
            DiscordSocketClient client,
            AudioConfig config,
            Func<LogMessage, Task> logger = null)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger ?? Extensions.NoOpLogger;

            Client.ReactionAdded += Client_ReactionAdded;
            if (Config.GuildConfigs.Count > 0)
            {
                Client.GuildAvailable += Client_GuildAvailable;
            }

            Log(LogSeverity.Info, "Created Audio service.");

            //_presenceChecker = new Timer(o =>
            //{
            //    foreach (var (guildId, wrapper) in Clients)
            //    {
            //        if (wrapper.Client.ConnectionState == ConnectionState.Disconnected)
            //        {
            //            Clients.TryRemove(guildId, out var _);
            //        }
            //    }
            //}, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
        }

        private Task Client_GuildAvailable(SocketGuild guild)
        {
            if (Config.GuildConfigs.TryGetValue(guild.Id, out var guildConfig) && guildConfig.AutoConnect)
            {
                var vChannel = guild.GetVoiceChannel(guildConfig.VoiceChannelId);
                var msgChannel = guild.GetTextChannel(guildConfig.MessageChannelId);
                if (vChannel != null && msgChannel != null)
                {
                    Task.Run(() => JoinAudio(guild, msgChannel, vChannel).ConfigureAwait(false));
                }
            }
            return Task.CompletedTask;
        }

        private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!message.HasValue)
            {
                return Log(LogSeverity.Debug, $"Message with id {message.Id} was not in cache.");
            }
            if (!reaction.User.IsSpecified)
            {
                return Log(LogSeverity.Debug, $"Message with id {message.Id} had an unspecified user.");
            }
            if (reaction.User.Value.Id == Client.CurrentUser.Id)
            {
                return Task.CompletedTask;
            }

            var msg = message.Value;
            if (!(msg.Channel is IGuildChannel gch))
            {
                return Task.CompletedTask;
            }

            if (Clients.TryGetValue(gch.GuildId, out var wrapper) && msg.Id == wrapper.Message.Id)
            {
                return UpdateWrapper(wrapper, msg, gch.Guild, reaction);
            }

            if (Lists.TryGetValue(msg.Id, out var embedList))
            {
                return UpdateEmbedList(embedList, reaction);
            }

            return Task.CompletedTask;
        }

        private async Task UpdateWrapper(
            AudioClientWrapper wrapper,
            IUserMessage msg,
            IGuild guild,
            SocketReaction reaction)
        {
            await msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value).ConfigureAwait(false);
            await GetAction(reaction.Emote).ConfigureAwait(false);

            Task GetAction(IEmote emote)
            {
                switch (emote.Name)
                {
                    case "\u25B6": //play
                        if (wrapper.IsPlaying())
                            return ResumePlayback(guild);

                        Task.Run(() => Playlist(guild).ConfigureAwait(false));
                        goto default;

                    case "\u23F8": //pause
                        return PausePlayback(guild);

                    case "\u23F9": //stop
                        StopPlaying(guild);
                        goto default;

                    case "\u23ED": //fwd
                        NextSong(guild);
                        goto default;

                    case "\u23CF": //eject
                        return LeaveAudio(guild);

                    default:
                        return Task.CompletedTask;
                }
            }
        }

        private async Task UpdateEmbedList(EmbedList embedList, SocketReaction reaction)
        {
            await embedList.Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value).ConfigureAwait(false);

            switch (reaction.Emote.Name)
            {
                case EmbedList.SFirst:
                    await embedList.First(reaction.User.Value).ConfigureAwait(false);
                    return;
                case EmbedList.SBack:
                    await embedList.Back(reaction.User.Value).ConfigureAwait(false);
                    return;
                case EmbedList.SNext:
                    await embedList.Next(reaction.User.Value).ConfigureAwait(false);
                    return;
                case EmbedList.SLast:
                    await embedList.Last(reaction.User.Value).ConfigureAwait(false);
                    return;
                case EmbedList.SDelete:
                    await embedList.Delete().ConfigureAwait(false);
                    Lists.TryRemove(embedList.Message.Id, out _);
                    return;
                default:
                    return;
            }
        }

        internal async Task JoinAudio(IGuild guild, IMessageChannel channel, IVoiceChannel target)
        {
            if (Clients.TryGetValue(guild.Id, out _))
            {
                return;
            }
            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            var guildConfig = Config.GuildConfigs.GetValueOrDefault(guild.Id);
            var audioClient = await target.ConnectAsync().ConfigureAwait(false);
            var message = await channel.SendMessageAsync("", embed: _initEmbed).ConfigureAwait(false);
            var wrapper = new AudioClientWrapper(audioClient, message, Config, guildConfig);

            if (Clients.TryAdd(guild.Id, wrapper))
            {
                await Log(LogSeverity.Info, $"Connected to voice channel '{target.Name}' on '{guild.Name}'.").ConfigureAwait(false);
                //audioClient.Connected += AudioClient_Connected;
                //audioClient.Disconnected += AudioClient_Disconnected;
            }

            var embedlist = new EmbedList(channel, this);
            if (Lists.TryAdd(embedlist.Message.Id, embedlist))
            {

            }

            if (guildConfig?.AutoPlay ?? Config.AutoPlay)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() => Playlist(guild).ConfigureAwait(false));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        //internal async Task SwitchAudio(IGuild guild, IVoiceChannel newTarget)
        //{
        //    if (newTarget.Guild.Id != guild.Id)
        //    {
        //        return;
        //    }
        //    if (Clients.TryGetValue(guild.Id, out var client))
        //    {
        //        if (client.IsPlaying())
        //        {
        //            return;
        //        }

        //        await LeaveAudio(guild);
        //        await JoinAudio(guild, newTarget);
        //    }
        //}

        internal async Task LeaveAudio(IGuild guild)
        {
            if (Clients.TryRemove(guild.Id, out var wrapper))
            {
                await wrapper.Client.StopAsync().ConfigureAwait(false);
                await wrapper.Message.DeleteAsync().ConfigureAwait(false);
                await Log(LogSeverity.Info, $"Disconnected from voice on '{guild.Name}'.").ConfigureAwait(false);
            }
        }

        internal async Task SendAudio(IGuild guild, IMessageChannel channel, string path)
        {
            if (!Clients.TryGetValue(guild.Id, out var acw))
            {
                await channel.SendMessageAsync("Not connected to voice in this guild.").ConfigureAwait(false);
                return;
            }
            try
            {
                var file = Config.MusicBasePath.EnumerateFiles($"*{path}*", SearchOption.AllDirectories).FirstOrDefault();

                if (file == null)
                {
                    await channel.SendMessageAsync("Could not find that file.").ConfigureAwait(false);
                    return;
                }
                if (Clients.TryGetValue(guild.Id, out var client))
                {
                    await client.AddToPlaylist(file).ConfigureAwait(false);
                    var fname = Path.GetFileNameWithoutExtension(file.FullName);
                    if (client.IsPlaying())
                    {
                        await Log(LogSeverity.Debug, $"Added '{fname}' to playlist in '{guild.Name}'").ConfigureAwait(false);
                        //await channel.SendMessageAsync($"Added `{file}` to the playlist.").ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        await Log(LogSeverity.Debug, $"Starting playback of '{file}' in '{guild.Name}'").ConfigureAwait(false);
                        //await channel.SendMessageAsync($"Now playing `{file}`.").ConfigureAwait(false);
                        await client.SendAudioAsync(Config.FFMpegPath).ConfigureAwait(false);
                    }
                }
            }
            finally
            {

            }
        }

        internal async Task PausePlayback(IGuild guild)
        {
            if (Clients.TryGetValue(guild.Id, out var client))
                await client.Pause().ConfigureAwait(false);
        }

        internal async Task ResumePlayback(IGuild guild)
        {
            if (Clients.TryGetValue(guild.Id, out var client))
                await client.Resume().ConfigureAwait(false);
        }

        internal async Task SetVolume(IGuild guild, float newVolume)
        {
            if (Clients.TryGetValue(guild.Id, out var client))
                await client.SetVolume(newVolume).ConfigureAwait(false);
        }

        internal void StopPlaying(IGuild guild)
        {
            if (Clients.TryGetValue(guild.Id, out var client))
                client.Stop();
        }

        internal void NextSong(IGuild guild)
        {
            if (Clients.TryGetValue(guild.Id, out var client))
                client.SkipToNext();
        }

        internal async Task Playlist(IGuild guild)
        {
            if (Clients.TryGetValue(guild.Id, out var client))
            {
                await client.AddToPlaylist(GetAvailableFiles().Shuffle(7)).ConfigureAwait(false);
                await client.SendAudioAsync(Config.FFMpegPath).ConfigureAwait(false);
            }
        }

        private static readonly ImmutableArray<string> _supportedExts = ImmutableArray.Create(
            ".mp3", ".flac", ".wav", ".aac", ".ogg");

        internal IEnumerable<FileInfo> GetAvailableFiles()
            => Config.MusicBasePath.EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(f => _supportedExts.Contains(f.Extension));

        private Task Log(LogSeverity severity, string msg)
        {
            return Logger(new LogMessage(severity, "AudioService", msg));
        }

        //private Task AudioClient_Connected(IAudioClient client)
        //{
        //    return Log(LogSeverity.Info, $"Connected to voice on {client.Guild.Name}!");
        //}

        //private Task AudioClient_Disconnected(IAudioClient client, Exception ex)
        //{
        //    if (ex == null || (ex is WebSocketException wse && wse.WebSocketErrorCode == WebSocketError.HeaderError))
        //    {
        //        client.Connected -= AudioClient_Connected;
        //        client.Disconnected -= AudioClient_Disconnected;
        //        ConnectedChannels.TryRemove(client.Guild.Id, out var _);
        //    }
        //    return Log(LogSeverity.Info, $"Disconnected from voice on {client.Guild.Name}!");
        //}
    }

    public static class AudioExt
    {
        //public static Task UseAudio<TModule>(
        //    this CommandService cmds,
        //    DiscordSocketClient client,
        //    IServiceCollection map,
        //    AudioConfig cfg,
        //    Func<LogMessage, Task> logger = null)
        //    where TModule : AudioModule
        //{
        //    map.AddSingleton(new AudioService(client, cfg, logger));
        //    return cmds.AddModuleAsync<TModule>();
        //}

        internal static bool HasPerms(this IGuildUser user, IGuildChannel channel, DiscordPermissions perms)
        {
            var clientPerms = (DiscordPermissions)user.GetPermissions(channel).RawValue;
            return (clientPerms & perms) == perms;
        }

        [Flags]
        internal enum DiscordPermissions : ulong
        {
            CREATE_INSTANT_INVITE = 0x00000001,
            KICK_MEMBERS = 0x00000002,
            BAN_MEMBERS = 0x00000004,
            ADMINISTRATOR = 0x00000008,
            MANAGE_CHANNELS = 0x00000010,
            MANAGE_GUILD = 0x00000020,
            ADD_REACTIONS = 0x00000040,
            READ_MESSAGES = 0x00000400,
            SEND_MESSAGES = 0x00000800,
            SEND_TTS_MESSAGES = 0x00001000,
            MANAGE_MESSAGES = 0x00002000,
            EMBED_LINKS = 0x00004000,
            ATTACH_FILES = 0x00008000,
            READ_MESSAGE_HISTORY = 0x00010000,
            MENTION_EVERYONE = 0x00020000,
            USE_EXTERNAL_EMOJIS = 0x00040000,
            CONNECT = 0x00100000,
            SPEAK = 0x00200000,
            MUTE_MEMBERS = 0x00400000,
            DEAFEN_MEMBERS = 0x00800000,
            MOVE_MEMBERS = 0x01000000,
            USE_VAD = 0x02000000,
            CHANGE_NICKNAME = 0x04000000,
            MANAGE_NICKNAMES = 0x08000000,
            MANAGE_ROLES = 0x10000000,
            MANAGE_WEBHOOKS = 0x20000000,
            MANAGE_EMOJIS = 0x40000000,
        }
    }
}
