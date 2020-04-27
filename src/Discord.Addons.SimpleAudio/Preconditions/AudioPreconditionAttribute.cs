using System;
using Discord.Commands;
using System.Threading.Tasks;

namespace Discord.Addons.SimpleAudio
{
    internal abstract class AudioPreconditionAttribute : PreconditionAttribute
    {
        protected async Task<bool> CheckAllowCommandsAsync(AudioService service, ICommandContext context)
        {
            var guildConfig = await service.Config.GetConfigForGuildAsync(context.Guild);
            return (guildConfig?.AllowCommands ?? service.Config.AllowReactions);
        }
    }
}
