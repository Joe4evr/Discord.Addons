namespace Discord.Addons.SimpleAudio
{
    /// <summary>
    /// </summary>
    public sealed class StandardAudioGuildConfig : IAudioGuildConfig
    {
        private ulong _voiceChannelId = 0UL;
        private ulong _messageChannelId = 0UL;
        private bool _autoConnect = false;
        private bool _autoPlay = false;
        private bool _allowCommands = true;
        private bool _allowReactions = true;
        private bool _showSongListOnJoin = false;

        /// <summary>
        ///     Sets the ID of the Voice channel in this guild to auto-connect to
        ///     if <see cref="AutoConnect"/> is <see langword="true"/>.
        /// </summary>
        public ulong VoiceChannelId { set => _voiceChannelId = value; }
        ulong IAudioGuildConfig.VoiceChannelId => _voiceChannelId;

        /// <summary>
        ///     Sets the ID of the Message channel in this guild to post
        ///     the player embed in if <see cref="AutoConnect"/> is <see langword="true"/>.
        /// </summary>
        public ulong MessageChannelId { set => _messageChannelId = value; }
        ulong IAudioGuildConfig.MessageChannelId => _messageChannelId;

        /// <summary>
        ///     Sets whether or not to auto connect to a voice channel
        ///     in this guild. Requires both <see cref="VoiceChannelId"/> and
        ///     <see cref="MessageChannelId"/> to be set to valid channel IDs.
        /// </summary>
        public bool AutoConnect { set => _autoConnect = value; }
        bool IAudioGuildConfig.AutoConnect => _autoConnect;

        /// <summary>
        ///     Sets whether or not to start auto-playing
        ///     songs upon connecting in this guild.
        /// </summary>
        public bool AutoPlay { set => _autoPlay = value; }
        bool IAudioGuildConfig.AutoPlay => _autoPlay;

        /// <summary>
        ///     Sets whether or not to allow control via commands in this guild.
        ///     Requires an implementation of <see cref="AudioModule"/>.
        /// </summary>
        public bool AllowCommands { set => _allowCommands = value; }
        bool IAudioGuildConfig.AllowCommands => _allowCommands;

        /// <summary>
        ///     Sets whether or not to allow control via reactions in this guild.
        /// </summary>
        public bool AllowReactions { set => _allowReactions = value; }
        bool IAudioGuildConfig.AllowReactions => _allowReactions;

        /// <summary>
        ///     Sets whether or not to show the available
        ///     list of songs right after joining a voice channel in this guild.
        /// </summary>
        public bool ShowSongListOnJoin { set => _showSongListOnJoin = value; }
        bool IAudioGuildConfig.ShowSongListOnJoin => _showSongListOnJoin;
    }
}
