using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.WebSocket;
using Discord.Addons.MpGame;

namespace Examples.MpGame
{
    public sealed class ExampleService : MpGameService<ExampleGame, Player>
    {
        public ExampleService(DiscordSocketClient client)
            : base(client) { }

        internal IReadOnlyDictionary<IMessageChannel, DataType> DataDictionary { get; }
    }

    internal class DataType
    {
    }
}
