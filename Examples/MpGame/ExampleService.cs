using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Addons.MpGame;

namespace Examples.MpGame
{
    public sealed class ExampleGameService : MpGameService<ExampleGame, ExamplePlayer>
    {
        public ExampleGameService(
            BaseSocketClient client,
            Func<LogMessage, Task> logger = null)
            : base(client, logger) { }

        internal IReadOnlyDictionary<IMessageChannel, DataType> DataDictionary { get; }
            = new Dictionary<IMessageChannel, DataType>(DiscordComparers.ChannelComparer);
    }

    internal class DataType
    {
    }
}
