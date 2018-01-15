using System;
using System.IO;
using System.Collections.Generic;

namespace Discord.Addons.SimpleAudio
{
    public class AudioConfig
    {
        public AudioConfig(string ffmpegPath, string musicBasePath)
        {
            if (!Directory.Exists(ffmpegPath))
                throw new ArgumentException(message: "Parameter must be a valid directory path", paramName: nameof(ffmpegPath));

            if (!Directory.Exists(musicBasePath))
                throw new ArgumentException(message: "Parameter must be a valid directory path", paramName: nameof(musicBasePath));

            FFMpegPath = ffmpegPath;
            MusicBasePath = musicBasePath;
        }

        public string FFMpegPath { get; }
        public string MusicBasePath { get; }
        public IDictionary<ulong, AudioGuildConfig> GuildConfigs { get; } = new Dictionary<ulong, AudioGuildConfig>();
    }

    public class AudioGuildConfig
    {
        public ulong VoiceChannelId { get; set; }
        public ulong MessageChannelId { get; set; }
        public bool AutoConnect { get; set; }
        public bool AutoPlay { get; set; }
        public bool AllowCommands { get; set; }
        public bool AllowReactions { get; set; }
    }
}
