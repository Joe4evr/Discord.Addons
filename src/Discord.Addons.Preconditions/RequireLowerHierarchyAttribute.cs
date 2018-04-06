using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Discord.Addons.Preconditions
{
    public sealed class RequireLowerHierarchyAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            ParameterInfo parameter,
            object value,
            IServiceProvider services)
        {
            if (value is SocketGuildUser user)
            {
                return ((context.Guild as SocketGuild).CurrentUser.Hierarchy > user.Hierarchy)
                    ? Task.FromResult(PreconditionResult.FromSuccess())
                    : Task.FromResult(PreconditionResult.FromError("Specified user must be lower in hierarchy."));
            }
            return Task.FromResult(PreconditionResult.FromError("Command requires Guild context."));
        }
    }
}
