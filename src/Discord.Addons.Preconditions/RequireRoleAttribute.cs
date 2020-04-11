using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.Preconditions
{
    /// <summary>
    ///     Indicates this command or all commands in this module can only
    ///     be executed if the user has the role with the specified Id.
    ///     This precondition automatically applies <see cref="RequireContextAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RequireRoleAttribute : RequireContextAttribute
    {
        private readonly ulong _requiredRole;

        /// <summary>
        /// </summary>
        /// <param name="requiredRole">
        /// </param>
        public RequireRoleAttribute(ulong requiredRole) : base(ContextType.Guild)
        {
            _requiredRole = requiredRole;
        }

        /// <inheritdoc />
        public override async Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var baseResult = await base.CheckPermissionsAsync(context, command, services);
            if (!baseResult.IsSuccess)
                return baseResult;

            return (((IGuildUser)context.User).RoleIds.Contains(_requiredRole))
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("User does not have the required role.");
        }
    }
}
