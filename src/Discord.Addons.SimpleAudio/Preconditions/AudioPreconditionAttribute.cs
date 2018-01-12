using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;

namespace Discord.Addons.SimpleAudio
{
    internal abstract class AudioPreconditionAttribute : PreconditionAttribute
    {
        protected bool CheckAllowCommands(AudioService service, ICommandContext context)
        {
            return service.Config.GuildConfigs.TryGetValue(context.Guild.Id, out var config)
                && config.AllowCommands;
        }
    }
}
