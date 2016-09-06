using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Sets the permission level of this command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PermissionAttribute : PreconditionAttribute
    {
        private MinimumPermission Permission { get; }

        /// <summary>
        /// Sets the permission level of this command.
        /// </summary>
        /// <param name="minimum">The <see cref="MinimumPermission"/> requirement.</param>
        public PermissionAttribute(MinimumPermission minimum)
        {
            Permission = minimum;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="executingCommand"></param>
        /// <param name="moduleInstance"></param>
        /// <returns></returns>
        public override Task<PreconditionResult> CheckPermissions(IUserMessage context, Command executingCommand, object moduleInstance)
        {
            var cfg = (moduleInstance as PermissionsModule)?.Config;
            if (cfg != null)
            {
                if (Permission <= MinimumPermission.BotOwner &&
                    context.Author.Id == cfg.OwnerId)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }

                var chan = context.Channel as IGuildChannel;
                var user = context.Author as IGuildUser;
                if (chan != null && user != null)
                {
                    if (Permission <= MinimumPermission.GuildOwner &&
                        user.Id == chan.Guild.OwnerId)
                    {
                            return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                    else if (cfg.ChannelModuleWhitelist[chan.Id].Contains(executingCommand.Module.Name))
                    {
                        if (Permission == MinimumPermission.Special &&
                            cfg.SpecialPermissionUsersList[chan.Id].Contains(user.Id))
                        {
                                return Task.FromResult(PreconditionResult.FromSuccess());
                        }
                        else if (Permission <= MinimumPermission.AdminRole &&
                            user.Roles.Any(r => r.Id == cfg.GuildAdminRole[chan.Guild.Id]))
                        {
                                return Task.FromResult(PreconditionResult.FromSuccess());
                        }
                        else if (Permission <= MinimumPermission.ModRole &&
                            user.Roles.Any(r => r.Id == cfg.GuildModRole[chan.Guild.Id]))
                        {
                                return Task.FromResult(PreconditionResult.FromSuccess());
                        }
                        else if (Permission == MinimumPermission.Everyone)
                        {
                            return Task.FromResult(PreconditionResult.FromSuccess());
                        }
                    }
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromError("Insufficient permission."));
                }
            }
            return Task.FromResult(PreconditionResult.FromError("No config found."));
        }
    }
}
