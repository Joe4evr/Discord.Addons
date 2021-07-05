using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Discord.Addons.SimpleAudio
{
    /// <summary>
    ///     Global configuration for SimpleAudio.
    /// </summary>
    public sealed class AudioConfig : IAudioConfig
    {
        private readonly IDictionary<ulong, IAudioGuildConfig> _guildConfigs = new Dictionary<ulong, IAudioGuildConfig>();

        /// <summary>
        ///     Initializes a new instance of the global configuration.
        /// </summary>
        /// <param name="ffmpegPath">
        ///     Path to 'ffmpeg.exe'.
        /// </param>
        /// <param name="musicBasePath">
        ///     Base path to find music files.
        /// </param>
        /// <exception cref="AggregateException">
        ///     Argument '<paramref name="ffmpegPath"/>' did not point to a valid file path
        ///     or '<paramref name="musicBasePath"/>' did not point to a valid directory path.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Argument '<paramref name="ffmpegPath"/>' did not point to 'ffmpeg.exe'.
        /// </exception>
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

        /// <inheritdoc />
        public FileInfo FFMpegPath { get; }

        /// <inheritdoc />
        public DirectoryInfo MusicBasePath { get; }

        /// <inheritdoc />
        public bool AutoPlay { get; set; } = false;

        /// <inheritdoc />
        public bool AllowCommands { get; set; } = true;

        /// <inheritdoc />
        public bool AllowReactions { get; set; } = true;

        /// <inheritdoc />
        public bool ShowSongListOnJoin { get; set; } = false;

        /// <inheritdoc />
        public Task<IAudioGuildConfig?> GetConfigForGuildAsync(IGuild guild)
            => (_guildConfigs.TryGetValue(guild.Id, out var conf))
                ? Task.FromResult<IAudioGuildConfig?>(conf)
                : Task.FromResult<IAudioGuildConfig?>(null);

        public AudioConfig AddGuildConfig(ulong guildId, IAudioGuildConfig config)
        {
            _guildConfigs.Add(guildId, config);
            return this;
        }
    }
}
