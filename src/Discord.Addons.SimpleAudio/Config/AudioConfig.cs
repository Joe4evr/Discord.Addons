using System;
using System.IO;
using System.Collections.Generic;

namespace Discord.Addons.SimpleAudio
{
    /// <summary>
    /// Global configuration for SimpleAudio.
    /// </summary>
    public sealed class AudioConfig
    {
        private IDictionary<ulong, IAudioGuildConfig> _guildConfigs = new Dictionary<ulong, IAudioGuildConfig>();

        /// <summary> Initializes a new instance of the global configuration. </summary>
        /// <param name="ffmpegPath">Path to 'ffmpeg.exe'.</param>
        /// <param name="musicBasePath">Base path to find music files.</param>
        /// <exception cref="AggregateException">Argument '<paramref name="ffmpegPath"/>' did not point to a valid file path
        /// or '<paramref name="musicBasePath"/>' did not point to a valid directory path.</exception>
        /// <exception cref="ArgumentException">Argument '<paramref name="ffmpegPath"/>' did not point to 'ffmpeg.exe'.</exception>
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

            if (!(FFMpegPath.Name == "ffmpeg.exe" || FFMpegPath.Name == "ffmpeg"))
                throw new ArgumentException(message: $"Argument '{nameof(ffmpegPath)}' must point to 'ffmpeg.exe'/'ffmpeg'.");

            try
            {
                MusicBasePath = new DirectoryInfo(Path.GetFullPath(musicBasePath));
            }
            catch (Exception ex)
            {
                throw new AggregateException(message: $"Parameter '{nameof(musicBasePath)}' must be a valid directory path.", innerException: ex);
            }
        }

        /// <summary> Gets the path to FFMpeg. </summary>
        public FileInfo FFMpegPath { get; }

        /// <summary> Gets the base path to find music files. </summary>
        public DirectoryInfo MusicBasePath { get; }

        /// <summary> Gets or sets a map of Guild IDs to Guild-specific configurations. </summary>
        /// <exception cref="ArgumentNullException">Value was set to <see langword="null"/>.</exception>
        public IDictionary<ulong, IAudioGuildConfig> GuildConfigs
        {
            get => _guildConfigs;
            set => _guildConfigs = (value ?? throw new ArgumentNullException($"May not set {nameof(GuildConfigs)} to 'null'."));
        }

        /// <summary> Gets or sets the global option of whether or
        /// not to start auto-playing songs upon connecting. </summary>
        public bool AutoPlay { get; set; } = false;

        /// <summary> Gets or sets the global option whether or not to allow control via commands.
        /// Requires an implementation of <see cref="AudioModule"/>. </summary>
        public bool AllowCommands { get; set; } = true;

        /// <summary> Gets or sets the global option of whether
        /// or not to allow control via reactions. </summary>
        public bool AllowReactions { get; set; } = true;

        /// <summary> Gets or sets the global option whether or not to show the available
        /// list of songs right after joining a voice channel. </summary>
        public bool ShowSongListOnJoin { get; set; } = false;
    }
}
