using System;

namespace Discord.Addons.MpGame
{
    //public partial interface IMpGameServiceConfig
    //{
    internal sealed class DefaultConfig : IMpGameServiceConfig
    {
        public static IMpGameServiceConfig Instance { get; } = new DefaultConfig();
        private DefaultConfig() { }

        ILogStrings IMpGameServiceConfig.LogStrings { get; } = DefaultLogStrings.Instance;
        //ILogStrings IMpGameServiceConfig.LogStrings { get; } = ILogStrings.Default;

        //bool IMpGameServiceConfig.AllowJoinMidGame  { get; } = false;
        //bool IMpGameServiceConfig.AllowLeaveMidGame { get; } = false;
    }
    //}
}
