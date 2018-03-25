using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Discord.Addons.SimplePermissions
{
    public sealed partial class PermissionsService
    {
        private sealed class CachedConfig : IPermissionConfig
        {
            private IEnumerable<ModuleInfo> Modules { get; }

            /// <summary> Gets whether fancy help messages are
            /// enabled in a specified guild. </summary>
            private Dictionary<ulong, bool> UseFancyHelps { get; }
                = new Dictionary<ulong, bool>();

            /// <summary> Gets the ID of the group that is considered
            /// the Admin role in a specified guild. </summary>
            private Dictionary<ulong, ulong> GuildAdminRole { get; }
                = new Dictionary<ulong, ulong>();

            /// <summary> Gets the ID of the group that is considered
            /// the Moderator role in a specified guild. </summary>
            private Dictionary<ulong, ulong> GuildModRole { get; }
                = new Dictionary<ulong, ulong>();

            /// <summary> Gets the list of modules that are
            /// whitelisted in a specified channel. </summary>
            private Dictionary<ulong, HashSet<ModuleInfo>> ChannelModuleWhitelist { get; }
                = new Dictionary<ulong, HashSet<ModuleInfo>>();

            /// <summary> Gets the list of modules that are
            /// whitelisted in a specified guild. </summary>
            private Dictionary<ulong, HashSet<ModuleInfo>> GuildModuleWhitelist { get; }
                = new Dictionary<ulong, HashSet<ModuleInfo>>();

            /// <summary> Gets the users that are allowed to use
            /// commands marked <see cref="MinimumPermission.Special"/>
            /// in a channel. </summary>
            private Dictionary<ulong, HashSet<ulong>> SpecialPermissionUsersList { get; }
                = new Dictionary<ulong, HashSet<ulong>>();

            private Dictionary<ulong, bool> HidePermCommandValues { get; }
                = new Dictionary<ulong, bool>();

            public CachedConfig(CommandService service)
            {
                Modules = service.Modules;
            }

            internal async Task Synchronize(BaseSocketClient client, IPermissionConfig sourceConfig)
            {
                foreach (var guild in client.Guilds)
                {
                    UseFancyHelps[guild.Id] = await sourceConfig.GetFancyHelpValue(guild);
                    GuildAdminRole[guild.Id] = sourceConfig.GetGuildAdminRole(guild);
                    GuildModRole[guild.Id] = sourceConfig.GetGuildModRole(guild);
                    GuildModuleWhitelist[guild.Id] = new HashSet<ModuleInfo>(sourceConfig.GetGuildModuleWhitelist(guild));
                    HidePermCommandValues[guild.Id] = await sourceConfig.GetHidePermCommands(guild);
                    foreach (var channel in guild.TextChannels)
                    {
                        ChannelModuleWhitelist[channel.Id] = new HashSet<ModuleInfo>(sourceConfig.GetChannelModuleWhitelist(channel));
                        SpecialPermissionUsersList[channel.Id] = new HashSet<ulong>(sourceConfig.GetSpecialPermissionUsersList(channel));
                    }
                }
            }

            IEnumerable<ModuleInfo> IPermissionConfig.GetChannelModuleWhitelist(ITextChannel channel)
            {
                return ChannelModuleWhitelist[channel.Id];
            }

            Task<bool> IPermissionConfig.GetFancyHelpValue(IGuild guild)
            {
                return Task.FromResult(UseFancyHelps[guild.Id]);
            }

            ulong IPermissionConfig.GetGuildAdminRole(IGuild guild)
            {
                return GuildAdminRole[guild.Id];
            }

            ulong IPermissionConfig.GetGuildModRole(IGuild guild)
            {
                return GuildModRole[guild.Id];
            }

            IEnumerable<ModuleInfo> IPermissionConfig.GetGuildModuleWhitelist(IGuild guild)
            {
                return GuildModuleWhitelist[guild.Id];
            }

            Task<bool> IPermissionConfig.GetHidePermCommands(IGuild guild)
            {
                return Task.FromResult(HidePermCommandValues[guild.Id]);
            }

            IEnumerable<ulong> IPermissionConfig.GetSpecialPermissionUsersList(ITextChannel channel)
            {
                return SpecialPermissionUsersList[channel.Id];
            }

            //writing operations
            async Task IPermissionConfig.AddNewGuild(IGuild guild)
            {
                if (!GuildAdminRole.ContainsKey(guild.Id))
                {
                    GuildAdminRole[guild.Id] = 0ul;
                }
                if (!GuildModRole.ContainsKey(guild.Id))
                {
                    GuildModRole[guild.Id] = 0ul;
                }
                if (!GuildModuleWhitelist.ContainsKey(guild.Id))
                {
                    GuildModuleWhitelist[guild.Id] = new HashSet<ModuleInfo>();
                }
                if (!UseFancyHelps.ContainsKey(guild.Id))
                {
                    UseFancyHelps[guild.Id] = false;
                }

                foreach (var channel in await guild.GetTextChannelsAsync())
                {
                    if (!ChannelModuleWhitelist.ContainsKey(channel.Id))
                    {
                        ChannelModuleWhitelist[channel.Id] = new HashSet<ModuleInfo>();
                    }
                    if (!SpecialPermissionUsersList.ContainsKey(channel.Id))
                    {
                        SpecialPermissionUsersList[channel.Id] = new HashSet<ulong>();
                    }
                }
            }

            Task IPermissionConfig.AddChannel(ITextChannel channel)
            {
                if (!ChannelModuleWhitelist.ContainsKey(channel.Id))
                {
                    ChannelModuleWhitelist[channel.Id] = new HashSet<ModuleInfo>();
                }
                if (!SpecialPermissionUsersList.ContainsKey(channel.Id))
                {
                    SpecialPermissionUsersList[channel.Id] = new HashSet<ulong>();
                }
                return Task.CompletedTask;
            }

            Task IPermissionConfig.AddUser(IGuildUser user)
            {
                return Task.CompletedTask;
            }

            Task<bool> IPermissionConfig.AddSpecialUser(ITextChannel channel, IGuildUser user)
            {
                return Task.FromResult(SpecialPermissionUsersList[channel.Id].Add(user.Id));
            }

            Task<bool> IPermissionConfig.RemoveSpecialUser(ITextChannel channel, IGuildUser user)
            {
                return Task.FromResult(SpecialPermissionUsersList[channel.Id].Remove(user.Id));
            }

            Task IPermissionConfig.SetHidePermCommands(IGuild guild, bool newValue)
            {
                HidePermCommandValues[guild.Id] = newValue;
                return Task.CompletedTask;
            }

            Task IPermissionConfig.RemoveChannel(ITextChannel channel)
            {
                if (ChannelModuleWhitelist.ContainsKey(channel.Id))
                {
                    ChannelModuleWhitelist.Remove(channel.Id);
                }
                if (SpecialPermissionUsersList.ContainsKey(channel.Id))
                {
                    SpecialPermissionUsersList.Remove(channel.Id);
                }

                return Task.CompletedTask;
            }

            Task IPermissionConfig.SetFancyHelpValue(IGuild guild, bool value)
            {
                UseFancyHelps[guild.Id] = value;
                return Task.CompletedTask;
            }

            Task<bool> IPermissionConfig.SetGuildAdminRole(IGuild guild, IRole role)
            {
                GuildAdminRole[guild.Id] = role.Id;
                return Task.FromResult(true);
            }

            Task<bool> IPermissionConfig.SetGuildModRole(IGuild guild, IRole role)
            {
                GuildModRole[guild.Id] = role.Id;
                return Task.FromResult(true);
            }

            Task<bool> IPermissionConfig.WhitelistModule(ITextChannel channel, ModuleInfo module)
            {
                return Task.FromResult(ChannelModuleWhitelist[channel.Id].Add(module));
            }

            Task<bool> IPermissionConfig.BlacklistModule(ITextChannel channel, ModuleInfo module)
            {
                return Task.FromResult(ChannelModuleWhitelist[channel.Id].Remove(module));
            }

            Task<bool> IPermissionConfig.WhitelistModuleGuild(IGuild guild, ModuleInfo module)
            {
                return Task.FromResult(GuildModuleWhitelist[guild.Id].Add(module));
            }

            Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, ModuleInfo module)
            {
                return Task.FromResult(GuildModuleWhitelist[guild.Id].Remove(module));
            }

            //no-op
            void IPermissionConfig.Save() { }
            void IDisposable.Dispose() { }
        }
    }
}
