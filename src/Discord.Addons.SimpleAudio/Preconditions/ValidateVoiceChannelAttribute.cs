using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimpleAudio
{
    internal sealed class ValidateVoiceChannelAttribute : ParameterPreconditionAttribute
    {
        private static readonly RequireContextAttribute _contextCheck = new RequireContextAttribute(ContextType.Guild);

        public override async Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            var isGuildResult = await _contextCheck.CheckPermissionsAsync(context, parameter.Command, services);
            if (!isGuildResult.IsSuccess)
                return isGuildResult;

            if (value is IVoiceChannel vc)
            {
                return ((await context.Guild.GetVoiceChannelsAsync()).Any(v => v.Id == vc.Id))
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError("Voice channel was not in this guild.");
            }
            return PreconditionResult.FromError("Object was not a voice channel.");
        }
    }
}
