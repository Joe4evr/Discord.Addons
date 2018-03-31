using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Contract for a configuration object used to determine command permissions. </summary>
    public interface IPermissionConfig : IDisposable
    {
        /// <summary> Set whether Fancy help is enabled in a specified guild. </summary>
        Task SetFancyHelpValue(IGuild guild, bool newValue);

        /// <summary> Get whether Fancy help is enabled in a specified guild. </summary>
        Task<bool> GetFancyHelpValue(IGuild guild);

        /// <summary> Add a new Guild (and all its Channels) to the config. </summary>
        Task AddNewGuild(IGuild guild);

        /// <summary> Add a new Channel to the config. </summary>
        Task AddChannel(ITextChannel channel);

        /// <summary> Removes the all lists of the specified channel from the config. </summary>
        Task RemoveChannel(ITextChannel channel);

        /// <summary> Gets the ID of the group that is considered
        /// the Admin role in a specified guild. </summary>
        IRole GetGuildAdminRole(IGuild guild);

        /// <summary> Gets the ID of the group that is considered
        /// the Moderator role in a specified guild. </summary>
        IRole GetGuildModRole(IGuild guild);

        /// <summary> Sets the ID of the group that is considered
        /// the Admin role in a specified guild. </summary>
        /// <returns><see cref="true"/> if the operation succeeded.</returns>
        Task<bool> SetGuildAdminRole(IGuild guild, IRole role);

        /// <summary> Sets the ID of the group that is considered
        /// the Moderator role in a specified guild. </summary>
        /// <returns><see cref="true"/> if the operation succeeded.</returns>
        Task<bool> SetGuildModRole(IGuild guild, IRole role);

        /// <summary> Gets the list of modules that are
        /// whitelisted in a specified channel. </summary>
        IEnumerable<ModuleInfo> GetChannelModuleWhitelist(ITextChannel channel);

        /// <summary> Gets the list of modules that are
        /// whitelisted in a specified guild. </summary>
        IEnumerable<ModuleInfo> GetGuildModuleWhitelist(IGuild guild);

        /// <summary> Whitelist a module in this channel. </summary>
        /// <returns><see cref="true"/> if the operation succeeded.</returns>
        Task<bool> WhitelistModule(ITextChannel channel, ModuleInfo module);

        /// <summary> Blacklist a module in this channel. </summary>
        /// <returns><see cref="true"/> if the operation succeeded.</returns>
        Task<bool> BlacklistModule(ITextChannel channel, ModuleInfo module);

        /// <summary> Whitelist a module in this guild. </summary>
        /// <returns><see cref="true"/> if the operation succeeded.</returns>
        Task<bool> WhitelistModuleGuild(IGuild guild, ModuleInfo module);

        /// <summary> Blacklist a module in this guild. </summary>
        /// <returns><see cref="true"/> if the operation succeeded.</returns>
        Task<bool> BlacklistModuleGuild(IGuild guild, ModuleInfo module);

        /// <summary> Gets the users that are allowed to use
        /// commands marked <see cref="MinimumPermission.Special"/>
        /// in a channel. </summary>
        IEnumerable<IGuildUser> GetSpecialPermissionUsersList(ITextChannel channel);

        /// <summary> Add a new user to the config. </summary>
        Task AddUser(IGuildUser user);

        /// <summary> Give a user Special command privileges in a channel. </summary>
        /// <returns><see cref="true"/> if the operation succeeded.</returns>
        Task<bool> AddSpecialUser(ITextChannel channel, IGuildUser user);

        /// <summary> Revoke a user's Special command privileges in a channel. </summary>
        /// <returns><see cref="true"/> if the operation succeeded.</returns>
        Task<bool> RemoveSpecialUser(ITextChannel channel, IGuildUser user);

        /// <summary> Sets whether to hide the Permission commands from help. </summary>
        Task SetHidePermCommands(IGuild guild, bool newValue);

        /// <summary> Gets whether the Permission commands are hidden from help. </summary>
        Task<bool> GetHidePermCommands(IGuild guild);

        /// <summary> Save the config. </summary>
        void Save();
    }
}
