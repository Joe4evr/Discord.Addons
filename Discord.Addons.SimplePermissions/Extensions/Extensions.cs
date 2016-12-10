using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    internal static class Extensions
    {
        public static async Task<IEnumerable<CommandInfo>> CheckConditions(
            this IEnumerable<CommandInfo> commands, CommandContext ctx, IDependencyMap map = null)
        {
            var ret = new List<CommandInfo>();
            foreach (var cmd in commands)
            {
                if ((await cmd.CheckPreconditionsAsync(ctx, map)).IsSuccess)
                {
                    ret.Add(cmd);
                }
            }
            return ret;
        }
    }
}
