using System;
using Discord;
using Discord.Addons.MpGame;

namespace Examples.MpGame
{
    public class ExamplePlayer : Player
    {
        public ExamplePlayer(IUser user, IMessageChannel channel)
            : base(user, channel)
        {
        }
    }
}
