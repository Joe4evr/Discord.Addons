using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    internal static class Extensions
    {
        public static async Task<IEnumerable<CommandInfo>> CheckConditions(
            this IEnumerable<CommandInfo> commands, ICommandContext ctx, IDependencyMap map = null)
        {
            var ret = new List<CommandInfo>();
            foreach (var cmd in commands)
            {
                if ((await cmd.CheckPreconditionsAsync(ctx, map).ConfigureAwait(false)).IsSuccess)
                {
                    ret.Add(cmd);
                }
            }
            return ret;
        }

        internal static EmbedBuilder AddFieldSequence<T>(
            this EmbedBuilder builder,
            IEnumerable<T> seq,
            Action<EmbedFieldBuilder, T> action)
        {
            foreach (var item in seq)
            {
                builder.AddField(efb => action(efb, item));
            }

            return builder;
        }

        internal static string ToNiceString(this TimeSpan ts)
        {
            var d = ts.TotalDays == 1 ? "day" : "days";
            var h = ts.Hours == 1 ? "hour" : "hours";
            var m = ts.Minutes == 1 ? "minute" : "minutes";

            return (ts.TotalHours > 24)
                ? $"{ts.TotalDays} {d} and {ts.Hours} {h}"
                : $"{ts.Hours} {h} and {ts.Minutes} {m}";
        }
    }
}
