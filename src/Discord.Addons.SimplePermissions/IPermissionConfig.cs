using System.Collections.Generic;
using System.Threading.Tasks;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Contract for a configuration object used to determine command permissions.
    /// </summary>
    public interface IPermissionConfig
    {
        /// <summary>
        /// Add a new Guild (and all its Channels) to the config.
        /// </summary>
        Task AddNewGuild(IGuild guild);

        /// <summary>
        /// Add a new Channel to the config.
        /// </summary>
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
        /// <returns>A <see cref="bool"/> indicating
        /// whether the operation succeeded</returns>
        Task<bool> SetGuildAdminRole(ulong guildId, IRole role);

        /// <summary>
        /// Sets the ID of the group that is considered
        /// the Moderator role in a specified guild.
        /// </summary>
        /// <returns>A <see cref="bool"/> indicating
        /// whether the operation succeeded</returns>
        Task<bool> SetGuildModRole(ulong guildId, IRole role);

        /// <summary>
        /// Gets the list of modules that are
        /// whitelisted in a specified channel.
        /// </summary>
        IEnumerable<string> GetChannelModuleWhitelist(ulong channelId);

        /// <summary>
        /// Whitelist a module in this channel.
        /// </summary>
        /// <returns>A <see cref="bool"/> indicating
        /// whether the operation succeeded</returns>
        Task<bool> WhitelistModule(ulong channelId, string moduleName);

        /// <summary>
        /// Blacklist a module in this channel.
        /// </summary>
        /// <returns>A <see cref="bool"/> indicating
        /// whether the operation succeeded</returns>
        Task<bool> BlacklistModule(ulong channelId, string moduleName);

        /// <summary>
        /// Gets the users that are allowed to use
        /// commands marked <see cref="MinimumPermission.Special"/>
        /// in a channel.
        /// </summary>
        IEnumerable<ulong> GetSpecialPermissionUsersList(ulong channelId);

        /// <summary>
        /// Give a user Special command privileges in a channel.
        /// </summary>
        /// <returns>A <see cref="bool"/> indicating
        /// whether the operation succeeded</returns>
        Task<bool> AddSpecialUser(ulong channelId, IUser user);

        /// <summary>
        /// Revoke a user's Special command privileges in a channel.
        /// </summary>
        /// <returns>A <see cref="bool"/> indicating
        /// whether the operation succeeded</returns>
        Task<bool> RemoveSpecialUser(ulong channelId, IUser user);
    }
}
