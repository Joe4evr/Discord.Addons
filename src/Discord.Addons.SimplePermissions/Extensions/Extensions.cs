using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using CommandParam = Discord.Commands.ParameterInfo;

namespace Discord.Addons.SimplePermissions
{
    internal static class DiscordExtensions
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

        internal static string FormatParam(this CommandParam param)
        {
            var sb = new StringBuilder();

            string type = param.Type.Name.StartsWith("Socket") ? param.Type.Name.Substring(6)
                : param.Type.GetTypeInfo().IsInterface ? param.Type.Name.Substring(1)
                : param.Type.Name;

            if (param.IsMultiple)
            {
                sb.Append($"`[({type}): {param.Name}...]`");
            }
            else if (param.IsRemainder) //&& IsOptional - decided not to check for the combination
            {
                sb.Append($"`<({type}): {param.Name}...>`");
            }
            else if (param.IsOptional)
            {
                sb.Append($"`[({type}): {param.Name}]`");
            }
            else
            {
                sb.Append($"`<({type}): {param.Name}>`");
            }

            if (!String.IsNullOrWhiteSpace(param.Summary))
            {
                sb.Append($" ({param.Summary})");
            }
            return sb.ToString();
        }
    }
}
