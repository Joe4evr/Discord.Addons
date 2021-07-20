using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.Net;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Sets the minimum permission level of this
    /// command or all commands in this module. Requires
    /// the module to be whitelisted for any level. </summary>
    /// <remarks> This precondition is always succesful
    /// outside of Guild contexts. Remember to apply
    /// <see cref="RequireContextAttribute"/> to restrict
    /// where commands may be invoked if needed. </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
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
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel is IPrivateChannel)
                return PreconditionResult.FromSuccess();

            var chan = (context.Channel as ITextChannel)!;
            var user = (context.User as IGuildUser)!;
            var config = services.GetService<IPermissionConfig>();
            if (config != null)
            {
                var adminRoleId = config.GetGuildAdminRole(context.Guild)?.Id;
                var modRoleId = config.GetGuildModRole(context.Guild)?.Id;

                var wlms = config.GetChannelModuleWhitelist(chan).Concat(config.GetGuildModuleWhitelist(context.Guild));
                if (IsModuleWhitelisted(wlms, command.Module))
                {
                    // Candidate switch expression
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
                        && config.GetSpecialPermissionUsersList(chan).Contains(user, DiscordComparers.UserComparer))
                    {
                        return PreconditionResult.FromSuccess();
                    }
                    else if (Permission <= MinimumPermission.GuildOwner
                        && context.Guild.OwnerId == user.Id)
                    {
                        return PreconditionResult.FromSuccess();
                    }
                    else if (Permission <= MinimumPermission.AdminRole
                        && adminRoleId.HasValue
                        && user.RoleIds.Contains(adminRoleId.Value))
                    {
                        return PreconditionResult.FromSuccess();
                    }
                    else if (Permission <= MinimumPermission.ModRole
                        && modRoleId.HasValue
                        && user.RoleIds.Contains(modRoleId.Value))
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
            else
            {
                return PreconditionResult.FromError("PermissionService not found.");
            }
        }

        private static bool IsModuleWhitelisted(IEnumerable<ModuleInfo> modules, ModuleInfo module)
        {
            return module.Name == PermissionsModule.PermModuleName ||
                modules.Any(m => m.Name == module.Name || IsModuleWhitelisted(m.Submodules, module));
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
