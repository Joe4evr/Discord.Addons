using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Indicates that this command should only be used while the user is in a voice channel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MustBeInVoiceAttribute : PreconditionAttribute
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
            return (context.User as IVoiceState)?.VoiceChannel == null ?
                Task.FromResult(PreconditionResult.FromError("Command must be invoked while in a voice channel.")) :
                Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
