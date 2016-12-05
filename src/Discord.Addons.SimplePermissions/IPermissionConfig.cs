using System.Collections.Generic;
using System.Threading.Tasks;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Contract for a configuration object used to determine command permissions.
    /// </summary>
    public interface IPermissionConfig
    {
        Task AddNewGuild(IGuild guild);

        Task AddChannel(IChannel channel);

        /// <summary>
        /// Removes the all lists of the specified channel from the config.
        /// </summary>
        Task RemoveChannel(ulong channelId);


        /// <summary>
        /// Gets the ID of the group that is considered
        /// the Admin role in a specified guild.
        /// </summary>
        ulong GetGuildAdminRole(ulong guildId);

        /// <summary>
        /// Gets the ID of the group that is considered
        /// the Moderator role in a specified guild.
        /// </summary>
        ulong GetGuildModRole(ulong guildId);

        /// <summary>
        /// Sets the ID of the group that is considered
        /// the Admin role in a specified guild.
        /// </summary>
        Task SetGuildAdminRole(ulong guildId, IRole role);

        /// <summary>
        /// Sets the ID of the group that is considered
        /// the Moderator role in a specified guild.
        /// </summary>
        Task SetGuildModRole(ulong guildId, IRole role);

        /// <summary>
        /// Gets the list of modules that are
        /// whitelisted in a specified channel.
        /// </summary>
        IEnumerable<string> GetChannelModuleWhitelist(ulong channelId);

        Task WhitelistModule(ulong channelId, string moduleName);

        Task BlacklistModule(ulong channelId, string moduleName);

        /// <summary>
        /// Gets the users that are allowed to use
        /// commands marked <see cref="MinimumPermission.Special"/>
        /// in a channel.
        /// </summary>
        IEnumerable<ulong> GetSpecialPermissionUsersList(ulong channelId);

        Task AddSpecialUser(ulong channelId, IUser user);

        Task RemoveSpecialUser(ulong channelId, IUser user);

    }
}
