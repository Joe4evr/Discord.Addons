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
        /// Gets the ID of the group that is considerd
        /// the Admin role in a specified guild.
        /// </summary>
        Dictionary<ulong, ulong> GuildAdminRole { get; }

        /// <summary>
        /// Gets the ID of the group that is considerd
        /// the Moderator role in a specified guild.
        /// </summary>
        Dictionary<ulong, ulong> GuildModRole { get; }

        /// <summary>
        /// Gets the list of commands that are
        /// whitelisted in a specified channel.
        /// </summary>
        Dictionary<ulong, List<string>> ChannelCommandWhitelist { get; }

        /// <summary>
        /// Gets the users that are allowed to use
        /// commands marked <see cref="MinimumPermission.Special"/>
        /// in a channel.
        /// </summary>
        Dictionary<ulong, List<ulong>> SpecialPermissionUsersList { get; }
    }
}
