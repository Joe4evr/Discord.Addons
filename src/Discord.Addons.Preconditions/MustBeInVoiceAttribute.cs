using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.Preconditions
{
    /// <summary> Indicates that this command should only be used while the user is in a voice channel. </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MustBeInVoiceAttribute : PreconditionAttribute
    {
        /// <inheritdoc />
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            return ((context.User as IVoiceState)?.VoiceChannel == null)
                ? Task.FromResult(PreconditionResult.FromError("Command must be invoked while in a voice channel."))
                : Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
