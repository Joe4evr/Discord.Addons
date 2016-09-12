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
        /// <param name="executingCommand"></param>
        /// <param name="moduleInstance"></param>
        /// <returns></returns>
        public async override Task<PreconditionResult> CheckPermissions(IUserMessage context, Command executingCommand, object moduleInstance)
        {
            var users = (await (context.Channel as IGuildChannel)?.Guild.GetUsersAsync()).Cast<IPresence>();
            return ((uint)users.Count(u => u.Status == UserStatus.Online) > MinimumUsers) ?
                 PreconditionResult.FromSuccess() :
                 PreconditionResult.FromError("Not enough users online.");
        }
    }
}
