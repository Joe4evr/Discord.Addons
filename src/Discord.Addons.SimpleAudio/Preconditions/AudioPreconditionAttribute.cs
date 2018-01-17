using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using Discord.Addons.Core;

namespace Discord.Addons.SimpleAudio
{
    internal abstract class AudioPreconditionAttribute : PreconditionAttribute
    {
        protected bool CheckAllowCommands(AudioService service, ICommandContext context)
        {
            var guildConfig = service.Config.GuildConfigs.GetValueOrDefault(context.Guild.Id);
            return (guildConfig?.AllowCommands ?? service.Config.AllowReactions);
        }
    }
}
