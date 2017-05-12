using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Addons.MpGame;

namespace Examples.MpGame
{
    public sealed class ExampleService : MpGameService<ExampleGame, Player>
    {
        internal IReadOnlyDictionary<IMessageChannel, DataType> DataDictionary { get; }
    }

    internal class DataType
    {
    }
}
