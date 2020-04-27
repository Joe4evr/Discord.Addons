using System;
using System.IO;
using System.Threading.Tasks;

namespace Discord.Addons.SimpleAudio
{
    /// <summary>
    ///     Global configuration for SimpleAudio.
    /// </summary>
    public interface IAudioConfig
    {
        /// <summary>
        ///     Gets the path to FFMpeg.
        /// </summary>
        FileInfo FFMpegPath { get; }

        /// <summary>
        ///     Gets the base path to find music files.
        /// </summary>
        DirectoryInfo MusicBasePath { get; }

        /// <summary>
        ///     Gets the global option of whether or
        ///     not to start auto-playing songs upon connecting.
        /// </summary>
        bool AutoPlay { get; }

        /// <summary>
        ///     Gets the global option whether or not to allow control via commands.
        ///     Requires an implementation of <see cref="AudioModule"/>.
        /// </summary>
        bool AllowCommands { get; }

        /// <summary>
        ///     Gets the global option of whether
        ///     or not to allow control via reactions.
        /// </summary>
        bool AllowReactions { get; }

        /// <summary>
        ///     Gets the global option whether or not to show the available
        ///     list of songs right after joining a voice channel.
        /// </summary>
        bool ShowSongListOnJoin { get; }

        /// <summary>
        ///     Gets the guild-specific configuration
        ///     associated with this guild.
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        Task<IAudioGuildConfig?> GetConfigForGuildAsync(IGuild guild);
    }
}
