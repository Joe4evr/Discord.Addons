using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons.MpGame
{
    public interface IMpGameServiceConfig
    {
        ILogStrings LogStrings { get; }

        //bool AllowJoinMidGame  { get; }
        //bool AllowLeaveMidGame { get; }
    }
}
