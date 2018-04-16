using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons.MpGame
{
    internal sealed class DefaultConfig : IMpGameServiceConfig
    {
        public static IMpGameServiceConfig Instance { get; } = new DefaultConfig();
        private DefaultConfig() { }

        ILogStrings IMpGameServiceConfig.LogStrings { get; } = DefaultLogStrings.Instance;

        //bool IMpGameServiceConfig.AllowJoinMidGame  { get; }
        //bool IMpGameServiceConfig.AllowLeaveMidGame { get; }
    }
}
