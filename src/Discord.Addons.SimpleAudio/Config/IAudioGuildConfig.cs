namespace Discord.Addons.SimpleAudio
{
    /// <summary>  </summary>
    public interface IAudioGuildConfig
    {
        /// <summary> Gets the ID of the Voice channel in this guild to auto-connect to
        /// if <see cref="AutoConnect"/> is <see langword="true"/>. </summary>
        ulong VoiceChannelId { get; }

        /// <summary> Gets the ID of the Message channel in this guild to post
        /// the player embed in if <see cref="AutoConnect"/> is <see langword="true"/>. </summary>
        ulong MessageChannelId { get; }

        /// <summary> Gets whether or not to auto connect to a voice channel
        /// in this guild. Requires both <see cref="VoiceChannelId"/> and
        /// <see cref="MessageChannelId"/> to be set to valid channel IDs. </summary>
        bool AutoConnect { get; }

        /// <summary> Gets whether or not to start auto-playing
        /// songs upon connecting in this guild. </summary>
        bool AutoPlay { get; }

        /// <summary> Gets whether or not to allow control via commands in this guild.
        /// Requires an implementation of <see cref="AudioModule"/>. </summary>
        bool AllowCommands { get; }

        /// <summary> Gets whether or not to allow control
        /// via reactions in this guild. </summary>
        bool AllowReactions { get; }

        /// <summary> Gets whether or not to show the available
        /// list of songs right after joining a voice channel in this guild. </summary>
        bool ShowSongListOnJoin { get; }
    }
}
