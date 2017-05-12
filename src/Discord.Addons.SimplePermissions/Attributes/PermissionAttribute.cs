using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Net;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Sets the permission level of this command. </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class PermissionAttribute : PreconditionAttribute
    {
        private MinimumPermission Permission { get; }

        /// <summary> Sets the permission level of this command. </summary>
        /// <param name="minimum">The <see cref="MinimumPermission"/> requirement.</param>
        public PermissionAttribute(MinimumPermission minimum)
        {
            Permission = minimum;
        }

        /// <inheritdoc />
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider map)
        {
            if (context.Channel is IPrivateChannel) return PreconditionResult.FromSuccess();

            var chan = context.Channel;
            var user = context.User;
            var svc = map.GetService<PermissionsService>();
            if (svc != null)
            {
                using (var config = svc.ConfigStore.Load())
                {

                    if (config.GetChannelModuleWhitelist(chan).Contains(command.Module.Name)
                        || config.GetGuildModuleWhitelist(context.Guild).Contains(command.Module.Name))
                    {
                        if (Permission == MinimumPermission.BotOwner)
                        {
                            try
                            {
                                var ownerId = (await context.Client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner.Id;
                                return user.Id == ownerId
                                    ? PreconditionResult.FromSuccess()
                                    : PreconditionResult.FromError("Insufficient permission.");
                            }
                            catch (HttpException)
                            {
                                return PreconditionResult.FromError("Not logged in as a bot.");
                            }
                        }
                        else if (Permission == MinimumPermission.Special
                            && config.GetSpecialPermissionUsersList(chan).Contains(user.Id))
                        {
                            return PreconditionResult.FromSuccess();
                        }
                        else if (Permission <= MinimumPermission.GuildOwner
                            && context.Guild?.OwnerId == user.Id)
                        {
                            return PreconditionResult.FromSuccess();
                        }
                        else if (Permission <= MinimumPermission.AdminRole
                            && (user as IGuildUser)?.RoleIds.Any(r => r == config.GetGuildAdminRole(context.Guild)) == true)
                        {
                            return PreconditionResult.FromSuccess();
                        }
                        else if (Permission <= MinimumPermission.ModRole
                            && (user as IGuildUser)?.RoleIds.Any(r => r == config.GetGuildModRole(context.Guild)) == true)
                        {
                            return PreconditionResult.FromSuccess();
                        }
                        else if (Permission == MinimumPermission.Everyone)
                        {
                            return PreconditionResult.FromSuccess();
                        }
                        else
                        {
                            return PreconditionResult.FromError("Insufficient permission.");
                        }
                    }
                    else
                    {
                        return PreconditionResult.FromError("Command not whitelisted");
                    }
                }
            }
            else
            {
                return PreconditionResult.FromError("PermissionService not found.");
            }
        }
    }

    /// <summary> Determines what a command's minimum permission requirement should be. </summary>
    public enum MinimumPermission
    {
        /// <summary> Everyone can use this command. </summary>
        Everyone = 0,

        /// <summary> People in the mod role or higher can use this command. </summary>
        ModRole = 1,

        /// <summary> People in the admin role or higher can use this command. </summary>
        AdminRole = 2,

        /// <summary> The guild owner can use this command. </summary>
        GuildOwner = 3,

        /// <summary> Someone is specially allowed to use this command. </summary>
        Special = 4,

        /// <summary> Only the bot owner can use this command. </summary>
        BotOwner = 5
    }
}
