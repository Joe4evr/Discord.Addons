using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    internal sealed class SpecialBoolTypeReader : TypeReader
    {
        private readonly IEnumerable<string> _trues;

        public SpecialBoolTypeReader()
        {
            _trues = new List<string>
            {
                "g",
                "guild",
                "global",
                true.ToString()
            };
        }

        public override Task<TypeReaderResult> Read(ICommandContext context, string input)
        {
            return Task.FromResult(TypeReaderResult.FromSuccess(_trues.Any(o => o.Equals(input, StringComparison.OrdinalIgnoreCase))));
        }
    }
}
