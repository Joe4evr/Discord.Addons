using System;
using System.Collections.Generic;
using System.Linq;
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
                if ((await cmd.CheckPreconditions(ctx, map)).IsSuccess)
                {
                    ret.Add(cmd);
                }
            }
            return ret;
        }

        public static IEnumerable<Overwrite> Resolve(this IEnumerable<Overwrite> perms, IGuildUser user)
        {
            foreach (var perm in perms)
            {
                if ((perm.TargetType == PermissionTarget.User && perm.TargetId == user.Id) ||
                    (perm.TargetType == PermissionTarget.Role && user.RoleIds.Contains(perm.TargetId)))
                {
                    yield return perm;
                }
            }
        }
    }
}
