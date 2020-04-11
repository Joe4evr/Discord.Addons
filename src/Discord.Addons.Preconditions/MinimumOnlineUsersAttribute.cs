using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.Preconditions
{
    /// <summary>
    ///     Sets how many users must be online in order to run the command.
    ///     This precondition ignores BOT accounts.
    ///     This precondition automatically applies <see cref="RequireContextAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class MinimumOnlineUsersAttribute : RequireContextAttribute
    {
        private readonly uint _minimumUsers;

        /// <summary>
        ///     Sets how many users must be online in order to run the command.
        /// </summary>
        /// <param name="minimumUsers">
        ///     The minimum amount of users that must be online.
        /// </param>
        public MinimumOnlineUsersAttribute(uint minimumUsers)
            : base(ContextType.Guild)
        {
            _minimumUsers = minimumUsers;
        }

        /// <inheritdoc />
        public override async Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var baseResult = await base.CheckPermissionsAsync(context, command, services);
            if (!baseResult.IsSuccess)
                return baseResult;

            var users = (await context.Guild.GetUsersAsync()).Where(u => !u.IsBot);
            return ((uint)users.Count(u => u.Status != UserStatus.Offline) >= _minimumUsers)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("Not enough users online.");
        }
    }
}
