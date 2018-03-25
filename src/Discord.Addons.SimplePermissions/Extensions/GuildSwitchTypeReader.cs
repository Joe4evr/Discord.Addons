using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    internal sealed class GuildSwitchTypeReader : TypeReader
    {
        private readonly HashSet<string> _trues;

        public GuildSwitchTypeReader()
        {
            _trues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "g",
                "-g",
                "guild",
                "-guild",
                "global",
                "-global",
                true.ToString()
            };
        }

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            return Task.FromResult(TypeReaderResult.FromSuccess(_trues.Contains(input)));
        }
    }
}
