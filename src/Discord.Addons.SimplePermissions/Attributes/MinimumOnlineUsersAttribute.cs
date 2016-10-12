using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Sets how many users must be online in order to run the command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MinimumOnlineUsersAttribute : PreconditionAttribute
    {
        private uint MinimumUsers { get; }

        /// <summary>
        /// Sets how many users must be online in order to run the command.
        /// </summary>
        /// <param name="minimumUsers">The minimum amount of users that must be online.</param>
        public MinimumOnlineUsersAttribute(uint minimumUsers)
        {
            MinimumUsers = minimumUsers;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public async override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            var users = (await context.Guild.GetUsersAsync()).Cast<IPresence>();
            return ((uint)users.Count(u => u.Status == UserStatus.Online) >= MinimumUsers) ?
                 PreconditionResult.FromSuccess() :
                 PreconditionResult.FromError("Not enough users online.");
        }
    }
}
