using System;

namespace Discord.Addons.MpGame
{
    public /*partial*/ interface IMpGameServiceConfig
    {
        //public static IMpGameServiceConfig Default { get; } = new DefaultConfig();

        ILogStrings LogStrings { get; }

        //bool AllowJoinMidGame  { get; }
        //bool AllowLeaveMidGame { get; }
    }
}
