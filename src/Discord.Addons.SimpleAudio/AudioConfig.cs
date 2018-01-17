using System;
using System.IO;
using System.Collections.Generic;

namespace Discord.Addons.SimpleAudio
{
    public class AudioConfig
    {
        public AudioConfig(string ffmpegPath, string musicBasePath)
        {
            try
            {
                FFMpegPath = new FileInfo(Path.GetFullPath(ffmpegPath));
            }
            catch (Exception ex)
            {
                throw new AggregateException(message: $"Parameter '{nameof(ffmpegPath)}' must be a valid file path.", innerException: ex);
            }

            if (FFMpegPath.Name != "ffmpeg.exe")
                throw new ArgumentException(message: $"Parameter '{nameof(ffmpegPath)}' must point to 'ffmpeg.exe'");

            try
            {
                MusicBasePath = new DirectoryInfo(Path.GetFullPath(musicBasePath));
            }
            catch (Exception ex)
            {
                throw new AggregateException(message: $"Parameter '{nameof(musicBasePath)}' must be a valid directory path.", innerException: ex);
            }
        }

        public FileInfo FFMpegPath { get; }
        public DirectoryInfo MusicBasePath { get; }
        public IDictionary<ulong, IAudioGuildConfig> GuildConfigs { get; } = new Dictionary<ulong, IAudioGuildConfig>();

        public bool AutoPlay       { get; set; } = false;
        public bool AllowCommands  { get; set; } = true;
        public bool AllowReactions { get; set; } = true;
    }

    public sealed class StandardAudioGuildConfig : IAudioGuildConfig
    {
        private ulong _voiceChannelId = 0UL;
        private ulong _messageChannelId = 0UL;
        private bool _autoConnect = false;
        private bool _autoPlay = false;
        private bool _allowCommands = true;
        private bool _allowReactions = true;

        /// <summary>
        /// ID of the Voice channel to auto-connect to
        /// if <see cref="AutoConnect"/> is <see langword="true"/>.
        /// </summary>
        public ulong VoiceChannelId { set => _voiceChannelId = value; }
        ulong IAudioGuildConfig.VoiceChannelId => _voiceChannelId;

        /// <summary>
        /// ID of the Message channel to post
        /// the player embed in if <see cref="AutoConnect"/> is <see langword="true"/>.
        /// </summary>
        public ulong MessageChannelId { set => _messageChannelId = value; }
        ulong IAudioGuildConfig.MessageChannelId => _messageChannelId;

        /// <summary>
        /// Set whether or not to auto connect to a voice channel
        /// on a particular guild.
        /// Requires both <see cref="VoiceChannelId"/> and
        /// <see cref="MessageChannelId"/> to be set to valid channel IDs.
        /// </summary>
        public bool AutoConnect { set => _autoConnect = value; }
        bool IAudioGuildConfig.AutoConnect => _autoConnect;

        /// <summary>
        /// Set whether or not to start auto-playing
        /// songs upon connecting.
        /// </summary>
        public bool AutoPlay { set => _autoPlay = value; }
        bool IAudioGuildConfig.AutoPlay => _autoPlay;

        /// <summary>
        /// Set whether or not to allow control via commands.
        /// Requires an implementation of <see cref="AudioModule"/>.
        /// </summary>
        public bool AllowCommands { set => _allowCommands = value; }
        bool IAudioGuildConfig.AllowCommands => _allowCommands;

        /// <summary>
        /// Set whether or not to allow control via reactions.
        /// </summary>
        public bool AllowReactions { set => _allowReactions = value; }
        bool IAudioGuildConfig.AllowReactions => _allowReactions;
    }

    public interface IAudioGuildConfig
    {
        //    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        //    [EditorBrowsable(EditorBrowsableState.Never)]
        //    [Browsable(false)]
        //    internal long _voiceChannelId;
        //    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        //    [EditorBrowsable(EditorBrowsableState.Never)]
        //    [Browsable(false)]
        //    internal long _messageChannelId;

        ulong VoiceChannelId { get; }

        ulong MessageChannelId { get; }

        bool AutoConnect { get; }

        bool AutoPlay { get; }

        bool AllowCommands { get; }

        bool AllowReactions { get; }
    }
}
