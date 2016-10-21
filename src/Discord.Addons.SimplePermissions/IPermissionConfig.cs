using System;
using System.Collections.Generic;
using Discord.Addons.SimpleConfig;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Contract for a configuration object used to determine command permissions.
    /// </summary>
    public interface IPermissionConfig : IConfig
    {
        /// <summary>
        /// Gets the ID of the group that is considered
        /// the Admin role in a specified guild.
        /// </summary>
        Dictionary<ulong, ulong> GuildAdminRole { get; }

        /// <summary>
        /// Gets the ID of the group that is considered
        /// the Moderator role in a specified guild.
        /// </summary>
        Dictionary<ulong, ulong> GuildModRole { get; }

        /// <summary>
        /// Gets the list of modules that are
        /// whitelisted in a specified channel.
        /// </summary>
        Dictionary<ulong, HashSet<string>> ChannelModuleWhitelist { get; }

        /// <summary>
        /// Gets the users that are allowed to use
        /// commands marked <see cref="MinimumPermission.Special"/>
        /// in a channel.
        /// </summary>
        Dictionary<ulong, HashSet<ulong>> SpecialPermissionUsersList { get; }
    }
}
