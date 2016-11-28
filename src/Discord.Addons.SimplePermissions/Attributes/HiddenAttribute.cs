using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Instructs the <see cref="PermissionsModule"/>'s help command to not
    /// display this particular command or overload. This is a marker attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HiddenAttribute : PreconditionAttribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
