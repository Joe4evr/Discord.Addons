﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Addons.Core;

namespace Discord.Addons.SimpleAudio
{
    public sealed class AudioService
    {
        private static readonly Embed _initEmbed = new EmbedBuilder { Title = "Connected", Description = "Initializing..." }.Build();
        private Func<LogMessage, Task> Logger { get; }
        //private readonly Timer _presenceChecker;
        private BaseSocketClient Client { get; }

        internal IAudioConfig Config { get; }
        internal ConcurrentDictionary<ulong, AudioClientWrapper> Clients { get; }
            = new ConcurrentDictionary<ulong, AudioClientWrapper>();

        internal ConcurrentDictionary<ulong, EmbedList> Lists { get; }
            = new ConcurrentDictionary<ulong, EmbedList>();

        public AudioService(
            BaseSocketClient client, IAudioConfig config,
            Func<LogMessage, Task>? logger = null)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger ?? Extensions.NoOpLogger;

            Client.ReactionAdded += Client_ReactionAdded;
            Client.GuildAvailable += Client_GuildAvailable;

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

        private async Task Client_GuildAvailable(SocketGuild guild)
        {
            var guildConfig = await Config.GetConfigForGuildAsync(guild);
            if (guildConfig != null && guildConfig.AutoConnect)
            {
                var vChannel = guild.GetVoiceChannel(guildConfig.VoiceChannelId);
                var msgChannel = guild.GetTextChannel(guildConfig.MessageChannelId);
                if (vChannel != null && msgChannel != null)
                {
                    _ = Task.Run(() => JoinAudio(guild, msgChannel, vChannel).ConfigureAwait(false));
                }
            }
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            //if (!message.HasValue)
            //{
            //    return Log(LogSeverity.Debug, $"Message with id {message.Id} was not in cache.");
            //}
            if (!reaction.User.IsSpecified)
            {
                await Log(LogSeverity.Debug, $"Message with id {message.Id} had an unspecified user.");
                return;
            }
            if (reaction.User.Value.Id == Client.CurrentUser.Id)
            {
                return;
            }

            var msg = await message.GetOrDownloadAsync();
            if (!(msg.Channel is IGuildChannel gch))
            {
                return;
            }

            // Please tell me this restriction on
            // patterns will lift in C# 9.0... ><
            if (GetWrapper(gch.Guild) is { } wrapper
                && msg.Id == wrapper.Message.Id)
            {
                await UpdateWrapper(wrapper, msg, gch.Guild, reaction);
                return;
            }

            if (Lists.TryGetValue(msg.Id, out var embedList))
            {
                await UpdateEmbedList(embedList, reaction);
                return;
            }
        }

        private async Task UpdateWrapper(
            AudioClientWrapper wrapper, IUserMessage msg,
            IGuild guild, SocketReaction reaction)
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


            var guildConfig = await Config.GetConfigForGuildAsync(guild);
            var audioClient = await target.ConnectAsync().ConfigureAwait(false);
            var message = await channel.SendMessageAsync("", embed: _initEmbed).ConfigureAwait(false);
            var wrapper = new AudioClientWrapper(audioClient, message, Config, guildConfig);

            if (Clients.TryAdd(guild.Id, wrapper))
            {
                await Log(LogSeverity.Info, $"Connected to voice channel '{target.Name}' on '{guild.Name}'.").ConfigureAwait(false);
                //audioClient.Connected += AudioClient_Connected;
                //audioClient.Disconnected += AudioClient_Disconnected;
            }

            if (guildConfig?.ShowSongListOnJoin ?? Config.ShowSongListOnJoin)
            {
                ListSongs(channel);
            }

            if (guildConfig?.AutoPlay ?? Config.AutoPlay)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() => Playlist(guild).ConfigureAwait(false));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        internal void ListSongs(IMessageChannel channel)
        {
            var embedlist = new EmbedList(channel, this);
            if (Lists.TryAdd(embedlist.Message.Id, embedlist))
            {
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
            if (!Clients.TryGetValue(guild.Id, out _))
            {
                await channel.SendMessageAsync("Not connected to voice in this guild.").ConfigureAwait(false);
                return;
            }

            try
            {
                var file = Config.MusicBasePath.EnumerateFiles($"*{path}*", SearchOption.AllDirectories)
                    .FirstOrDefault();
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

        internal AudioClientWrapper? GetWrapper(IGuild guild)
        {
            return Clients.TryGetValue(guild.Id, out var client)
                ? client : null;
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

        private static readonly ImmutableArray<string> _supportedExts
            = ImmutableArray.Create(".mp3", ".flac"/*, ".wav", ".aac", ".ogg"*/);

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
}
