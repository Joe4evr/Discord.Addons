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
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            var guildConfig = service.Config.GuildConfigs.GetValueOrDefault(context.Guild.Id);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            return (guildConfig?.AllowCommands ?? service.Config.AllowReactions);
        }
    }
}
