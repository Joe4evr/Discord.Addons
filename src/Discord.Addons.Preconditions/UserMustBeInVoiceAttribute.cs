using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.Preconditions
{
    /// <summary>
    ///     Indicates that this command should only be used while the user is in a voice channel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class UserMustBeInVoiceAttribute : PreconditionAttribute
    {
        /// <inheritdoc />
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var current = (context.User as IVoiceState)?.VoiceChannel?.Id;
            return (await context.Guild.GetVoiceChannelsAsync()).Any(v => v.Id == current)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("Command must be invoked while in a voice channel in this guild.");
        }
    }
}
