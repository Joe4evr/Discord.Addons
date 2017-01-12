using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.Preconditions
{
    /// <summary> Sets how many users must be online in order to run the command.
    /// This precondition ignores BOT accounts. </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MinimumOnlineUsersAttribute : PreconditionAttribute
    {
        private readonly uint _minimumUsers;

        /// <summary> Sets how many users must be online in order to run the command. </summary>
        /// <param name="minimumUsers">The minimum amount of users that must be online.</param>
        public MinimumOnlineUsersAttribute(uint minimumUsers)
        {
            _minimumUsers = minimumUsers;
        }

        /// <inheritdoc />
        public async override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            var users = (await context.Guild.GetUsersAsync()).Where(u => !u.IsBot);

            return ((uint)users.Count(u => u.Status == UserStatus.Online || u.Status == UserStatus.Idle) >= _minimumUsers)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("Not enough users online.");
        }
    }
}
