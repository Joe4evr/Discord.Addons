using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Discord.Addons.Preconditions
{
    /// <summary>
    ///     Indicates this parameter must be a <see cref="SocketGuildUser"/>
    ///     whose <see cref="SocketGuildUser.Hierarchy"/> value must be
    ///     lower than that of the Bot.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class RequireLowerHierarchyAttribute : ParameterPreconditionAttribute
    {
        /// <summary>
        /// </summary>
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext _, ParameterInfo __, object value, IServiceProvider ___)
        {
            if (value is SocketGuildUser user)
            {
                return (user.Guild.CurrentUser.Hierarchy > user.Hierarchy)
                    ? Task.FromResult(PreconditionResult.FromSuccess())
                    : Task.FromResult(PreconditionResult.FromError("Specified user must be lower in hierarchy."));
            }
            return Task.FromResult(PreconditionResult.FromError("Command requires Guild context."));
        }
    }
}
