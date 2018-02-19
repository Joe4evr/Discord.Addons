using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Addons.MpGame;

namespace Examples.MpGame
{
    public sealed class ExampleGameService : MpGameService<ExampleGame, ExamplePlayer>
    {
        public ExampleGameService(DiscordSocketClient client)
            : base(client) { }

        internal IReadOnlyDictionary<IMessageChannel, DataType> DataDictionary { get; }
    }

    internal class DataType
    {
    }
}
