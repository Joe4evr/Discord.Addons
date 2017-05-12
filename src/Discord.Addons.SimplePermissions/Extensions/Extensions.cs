using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using CommandParam = Discord.Commands.ParameterInfo;

namespace Discord.Addons.SimplePermissions
{
    internal static class Extensions
    {
        public static async Task<IEnumerable<CommandInfo>> CheckConditions(
            this IEnumerable<CommandInfo> commands,
            ICommandContext ctx,
            IServiceProvider svcs,
            PermissionsService permsvc)
        {
            var ret = new List<CommandInfo>();
            foreach (var cmd in commands)
            {
                if ((await cmd.CheckPreconditionsAsync(ctx, svcs).ConfigureAwait(false)).IsSuccess)
                {
                    if (cmd.Module.Name == PermissionsModule.PermModuleName
                        && cmd.Name != nameof(PermissionsModule.HelpCmd)
                        && !(await permsvc.GetHidePermCommands(ctx.Guild).ConfigureAwait(false)))
                    {
                        ret.Add(cmd);
                    }
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
                ? $"{ts.Days} {d} and {ts.Hours} {h}"
                : $"{ts.Hours} {h} and {ts.Minutes} {m}";
        }

        internal static string FormatParam(this CommandParam param)
        {
            var sb = new StringBuilder();
            if (param.IsMultiple)
            {
                sb.Append($"`[({param.Type.Name}): {param.Name}...]`");
            }
            else if (param.IsRemainder) //&& IsOptional - decided not to check for the combination
            {
                sb.Append($"`<({param.Type.Name}): {param.Name}...>`");
            }
            else if (param.IsOptional)
            {
                sb.Append($"`[({param.Type.Name}): {param.Name}]`");
            }
            else
            {
                sb.Append($"`<({param.Type.Name}): {param.Name}>`");
            }

            if (!String.IsNullOrWhiteSpace(param.Summary))
            {
                sb.Append($" ({param.Summary})");
            }
            return sb.ToString();
        }
    }
}
