using System.Collections.Generic;

namespace Discord.Addons.SimpleAudio
{
    public class AudioConfig
    {
        public string FFMpegPath { get; set; } = "";
        public string MusicBasePath { get; set; } = "";
        public IDictionary<ulong, AudioGuildConfig> GuildConfigs { get; set; } = new Dictionary<ulong, AudioGuildConfig>();
    }

    public class AudioGuildConfig
    {
        public ulong VoiceChannelId { get; set; }
        public ulong MessageChannelId { get; set; }
        public bool AutoPlay { get; set; }
        public bool AllowCommands { get; set; }
        public bool AllowReactions { get; set; }
    }
}
