using System;

namespace Discord.Addons.MpGame
{
    public partial interface IMpGameServiceConfig
    {
#if NETCOREAPP3_0
        /// <summary>
        ///     The default config if none specified.
        /// </summary>
        public static IMpGameServiceConfig Default { get; } = new DefaultConfig();
        
        private sealed class DefaultConfig : IMpGameServiceConfig
        {
#else
        internal sealed class DefaultConfig : IMpGameServiceConfig
        {
            public static IMpGameServiceConfig Instance { get; } = new DefaultConfig();

            private DefaultConfig() { }
#endif
            /// <inheritdoc/>
            ILogStrings IMpGameServiceConfig.LogStrings { get; }
#if NETCOREAPP3_0
                = ILogStrings.Default;
#else
                = ILogStrings.DefaultLogStrings.Instance;
#endif

            /// <inheritdoc/>
            bool IMpGameServiceConfig.AllowJoinMidGame { get; } = false;

            /// <inheritdoc/>
            bool IMpGameServiceConfig.AllowLeaveMidGame { get; } = false;
        }
    }
}
