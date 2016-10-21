using System;
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
        /// <param name="command"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            var cfg = map.Get<PermissionsService>().ConfigStore.Load();
            var chan = context.Channel;
            var user = context.User;

            if (cfg != null)
            {
                if (Permission == MinimumPermission.BotOwner &&
                    user.Id == cfg.OwnerId)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }

                if (cfg.ChannelModuleWhitelist[chan.Id].Contains(command.Module.Source.FullName))
                {
                    if (Permission == MinimumPermission.Special &&
                        cfg.SpecialPermissionUsersList[chan.Id].Contains(user.Id))
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                    else if (Permission <= MinimumPermission.GuildOwner &&
                        context.Guild?.OwnerId == user.Id)
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                    else if (Permission <= MinimumPermission.AdminRole &&
                        (user as IGuildUser)?.RoleIds.Any(r => r == cfg.GuildAdminRole[context.Guild.Id]) == true)
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                    else if (Permission <= MinimumPermission.ModRole &&
                        (user as IGuildUser)?.RoleIds.Any(r => r == cfg.GuildModRole[context.Guild.Id]) == true)
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                    else if (Permission == MinimumPermission.Everyone)
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                    else
                    {
                        return Task.FromResult(PreconditionResult.FromError("Insufficient permission."));
                    }
                }
                else
                    return Task.FromResult(PreconditionResult.FromError("Command not whitelisted"));
            }
            else
                return Task.FromResult(PreconditionResult.FromError("No config found."));
        }
    }
}
