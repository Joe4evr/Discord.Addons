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

namespace Discord.Addons.SimpleAudio
{
    public sealed class AudioService
    {
        private static readonly Embed _rdyEmbed = new EmbedBuilder { Title = "Connected", Description = "Ready" }.Build();
        private readonly AudioConfig _config;
        private readonly Func<LogMessage, Task> _logger;
        private readonly Timer _presenceChecker;

        internal ConcurrentDictionary<ulong, AudioClientWrapper> Clients { get; }
            = new ConcurrentDictionary<ulong, AudioClientWrapper>();

        internal AudioService(
            AudioConfig config,
            Func<LogMessage, Task> logger)
        {
            _config = config;
            _logger = logger;
            Log(LogSeverity.Info, "Created Audio service.");
            _presenceChecker = new Timer(o =>
            {
                foreach (var (guildId, wrapper) in Clients)
                {
                    if (wrapper.Client.ConnectionState == ConnectionState.Disconnected)
                    {
                        Clients.TryRemove(guildId, out var _);
                    }
                }
            }, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
        }

        internal async Task JoinAudio(IGuild guild, IMessageChannel channel, IVoiceChannel target)
        {
            if (Clients.TryGetValue(guild.Id, out var _))
            {
                return;
            }
            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            var audioClient = await target.ConnectAsync().ConfigureAwait(false);
            var wrapper = new AudioClientWrapper(audioClient, await channel.SendMessageAsync("", embed: _rdyEmbed));

            if (Clients.TryAdd(guild.Id, wrapper))
            {
                await Log(LogSeverity.Info, $"Connected to voice channel '{target.Name}' on '{guild.Name}'.").ConfigureAwait(false);
                //audioClient.Connected += AudioClient_Connected;
                //audioClient.Disconnected += AudioClient_Disconnected;
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
                await Log(LogSeverity.Info, $"Disconnected from voice on '{guild.Name}'.").ConfigureAwait(false);
            }
        }

        internal async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string path)
        {
            if (!Clients.TryGetValue(guild.Id, out var acw))
            {
                await channel.SendMessageAsync("Not connected to voice in this guild.").ConfigureAwait(false);
                return;
            }

            string file = Array.Find(Directory.GetFiles(_config.MusicBasePath),
                f => Path.GetFileNameWithoutExtension(f).Equals(path, StringComparison.OrdinalIgnoreCase));

            if (file == null)
            {
                await channel.SendMessageAsync("Could not find that file.").ConfigureAwait(false);
                return;
            }
            if (Clients.TryGetValue(guild.Id, out var client))
            {
                await client.AddToPlaylist(file).ConfigureAwait(false);
                file = Path.GetFileNameWithoutExtension(file);
                if (client.IsPlaying())
                {
                    await Log(LogSeverity.Debug, $"Added '{file}' to playlist in '{guild.Name}'").ConfigureAwait(false);
                    //await channel.SendMessageAsync($"Added `{file}` to the playlist.").ConfigureAwait(false);
                    return;
                }
                else
                {
                    await Log(LogSeverity.Debug, $"Starting playback of '{file}' in '{guild.Name}'").ConfigureAwait(false);
                    //await channel.SendMessageAsync($"Now playing `{file}`.").ConfigureAwait(false);
                    await client.SendAudioAsync(Path.Combine(_config.FFMpegPath, "ffmpeg.exe")).ConfigureAwait(false);
                }
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
                client.Cancel();
        }

        internal void NextSong(IGuild guild)
        {
            if (Clients.TryGetValue(guild.Id, out var client))
                client.SkipToNext();
        }

        internal IEnumerable<string> GetAvailableFiles()
            => Directory.GetFiles(_config.MusicBasePath, "*.mp3", SearchOption.TopDirectoryOnly).Select(p => Path.GetFileName(p));

        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "AudioService", msg));
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
        public static Task UseAudio<TModule>(
            this CommandService cmds,
            IServiceCollection map,
            AudioConfig cfg,
            Func<LogMessage, Task> logger = null)
            where TModule : AudioModule
        {
            map.AddSingleton(new AudioService(cfg, logger ?? ((msg) => Task.CompletedTask)));
            return cmds.AddModuleAsync<TModule>();
        }

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
