using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.Preconditions
{
    /// <summary>
    ///     Indicates this command or all commands in this module can only
    ///     be executed if the user has the role with the specified Id.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RequireRoleAttribute : RequireContextAttribute
    {
        private readonly ulong _requiredRole;

        public RequireRoleAttribute(ulong requiredRole) : base(ContextType.Guild)
        {
            _requiredRole = requiredRole;
        }

        /// <inheritdoc />
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var baseResult = await base.CheckPermissionsAsync(context, command, services);
            if (baseResult.IsSuccess && ((IGuildUser)context.User).RoleIds.Contains(_requiredRole))
            {
                return PreconditionResult.FromSuccess();
            }
            else
            {
                return baseResult;
            }
        }
    }

    //class Test
    //{
    //    [RequireContext(ContextType.Guild)]
    //    [RequireRole(12345ul)]
    //    void M()
    //    {
    //    }
    //}
}
